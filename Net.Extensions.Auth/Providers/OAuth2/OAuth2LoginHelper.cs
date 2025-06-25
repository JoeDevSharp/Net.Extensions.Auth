using Net.Extensions.Auth.Core;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Net.Extensions.Auth.Providers.OAuth2
{
    internal static class OAuth2LoginHelper
    {
        public static string GetCodeViaBrowser(string authUrl, string redirectUri)
        {
            // Asegura el slash final
            var prefix = redirectUri.EndsWith("/") ? redirectUri : redirectUri + "/";

            using var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            Console.WriteLine("🌐 Esperando conexión en: " + prefix);

            // Abrir navegador
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            // Espera la petición (bloqueante)
            var context = listener.GetContext();
            Console.WriteLine("✅ Conexión recibida.");

            // Leer query
            var query = context.Request.Url?.Query ?? "";
            var queryParams = HttpUtility.ParseQueryString(query);
            var code = queryParams["code"];

            // Enviar respuesta HTML al navegador
            var html = "<html><body><h2>✅ Autenticación completada</h2><p>Puede cerrar esta ventana.</p></body></html>";
            var buffer = Encoding.UTF8.GetBytes(html);

            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
            context.Response.Close();

            listener.Stop();

            if (string.IsNullOrWhiteSpace(code))
                throw new Exception("❌ No se recibió el código de autorización.");

            return code;
        }

        public static async Task<OAuth2Token> ExchangeCodeForTokenAsync(string code, OAuth2Options options)
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            // Construir correctamente los datos como en Postman
            var dict = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri,
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret
            };

            // Enviar solicitud al token endpoint
            Console.WriteLine("Enviando POST al TokenEndpoint...");
            var response = await client.PostAsync(options.TokenEndpoint, new FormUrlEncodedContent(dict));
            Console.WriteLine("POST finalizado, leyendo contenido...");
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Contenido recibido: " + json);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"❌ Error al obtener el token: {content}");

            var payload = JsonDocument.Parse(content).RootElement;

            return new OAuth2Token
            {
                AccessToken = payload.GetProperty("access_token").GetString() ?? "",
                RefreshToken = payload.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                ExpiresAt = DateTime.UtcNow.AddSeconds(payload.GetProperty("expires_in").GetInt32())
            };
        }


        public static async Task<AuthUser> GetUserInfoAsync(string accessToken, string userInfoUrl = "")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = string.IsNullOrWhiteSpace(userInfoUrl)
                ? "https://openidconnect.googleapis.com/v1/userinfo" // por defecto, Google
                : userInfoUrl;

            var json = await client.GetStringAsync(url);
            var data = JsonDocument.Parse(json).RootElement;

            return new AuthUser
            {
                Id = data.GetProperty("sub").GetString() ?? "",
                Username = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Email = data.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                Roles = new List<string>(), // muchos OIDC no incluyen roles aquí
                Claims = data.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString())
            };
        }
    }

}
