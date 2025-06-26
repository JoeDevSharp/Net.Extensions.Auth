using Net.Extensions.OAuth2.Enums;
using Net.Extensions.OAuth2.Interfaces;
using Net.Extensions.OAuth2.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Net.Extensions.OAuth2
{
    internal static class OAuth2Helper
    {
        public static string GetCodeViaBrowser(string authUrl, string redirectUri)
        {
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

            // Respuesta HTML que cierra la pestaña automáticamente
            var html = @"
                <html>
                    <body>
                        <h2>✅ Autenticación completada</h2>
                        <p>Puede cerrar esta ventana.</p>
                        <script>
                            // Intentar cerrar la pestaña después de 1 segundo
                            setTimeout(() => {
                                window.open('', '_self', '');
                                window.close();
                            }, 1000);
                        </script>
                    </body>
                </html>";

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

            var request = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(dict)
            };

            // Añade Accept: application/json solo si se espera JSON
            if (options.TokenResponseFormat == OAuth2TokenResponseFormat.Json)
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error obteniendo token: {content}");

            OAuth2Token token;

            if (options.TokenResponseFormat == OAuth2TokenResponseFormat.Json)
            {
                var payload = JsonDocument.Parse(content).RootElement;
                token = new OAuth2Token
                {
                    AccessToken = payload.GetProperty("access_token").GetString() ?? "",
                    RefreshToken = payload.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
                    ExpiresAt = DateTime.UtcNow.AddSeconds(payload.GetProperty("expires_in").GetInt32())
                };
            }
            else // FormUrlEncoded
            {
                var parsed = HttpUtility.ParseQueryString(content);
                token = new OAuth2Token
                {
                    AccessToken = parsed["access_token"] ?? "",
                    RefreshToken = parsed["refresh_token"] ?? "",
                    ExpiresAt = DateTime.UtcNow.AddSeconds(int.TryParse(parsed["expires_in"], out var sec) ? sec : 3600)
                };
            }

            return token;
        }
    }
}
