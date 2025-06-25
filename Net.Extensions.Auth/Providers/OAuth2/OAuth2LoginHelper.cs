using Net.Extensions.Auth.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Net.Extensions.Auth.Providers.OAuth2
{
    internal static class OAuth2LoginHelper
    {
        public static async Task<string> GetCodeViaBrowserAsync(string authUrl, string redirectUri)
        {
            var prefix = redirectUri.EndsWith("/") ? redirectUri : redirectUri + "/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            var context = await listener.GetContextAsync();
            var query = context.Request.Url?.Query ?? "";

            var responseHtml = "<html><body>✅ Autenticación completada. Puede cerrar esta ventana.</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseHtml);

            var response = context.Response;
            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close(); // 🔴 MUY IMPORTANTE
            response.Close();              // 🔴 Cierra correctamente la respuesta

            var queryParams = HttpUtility.ParseQueryString(query);
            return queryParams["code"] ?? throw new Exception("No se recibió el código de autorización.");
        }


        public static async Task<OAuth2Token> ExchangeCodeForTokenAsync(string code, OAuth2Options options)
        {
            using var client = new HttpClient();

            var dict = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri,
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret
            };

            var response = await client.PostAsync(options.TokenEndpoint, new FormUrlEncodedContent(dict));
            var json = await client.GetStringAsync(options.UserInfoEndpoint);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error obteniendo token: {json}");

            var payload = JsonDocument.Parse(json).RootElement;

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
