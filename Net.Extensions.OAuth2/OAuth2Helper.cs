using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Net.Extensions.OAuth2.Enums;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Net.Extensions.OAuth2
{
    public static class OAuth2Helper
    {
        public static async Task<string> GetCodeViaLocalServerAsync(string authUrl, string redirectUri, CancellationToken cancellationToken = default)
        {
            var uri = new Uri(redirectUri);
            int port = uri.Port;
            string path = uri.AbsolutePath;

            var tcs = new TaskCompletionSource<string>();

            var webHost = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:{port}")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        if (context.Request.Path == path)
                        {
                            var code = context.Request.Query["code"];
                            if (!string.IsNullOrEmpty(code))
                            {
                                tcs.TrySetResult(code);
                                await context.Response.WriteAsync("Authorization code received. You can close this window.");
                            }
                            else
                            {
                                await context.Response.WriteAsync("No code received.");
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("Not found");
                        }
                    });
                })
                .Build();

            await webHost.StartAsync(cancellationToken);

            // Abre el navegador al URL de autorización
            OpenBrowser(authUrl);

            using (cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
            }))
            {
                try
                {
                    var code = await tcs.Task;
                    await webHost.StopAsync();
                    webHost.Dispose();
                    return code!;
                }
                catch
                {
                    await webHost.StopAsync();
                    webHost.Dispose();
                    throw;
                }
            }
        }

        // Método simple para abrir el navegador
        private static void OpenBrowser(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignorar errores
            }
        }

        public static string GenerateCodeVerifier(int length = 64)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._~";
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            var result = new StringBuilder(length);
            foreach (var b in bytes)
                result.Append(chars[b % chars.Length]);

            return result.ToString();
        }

        public static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.ASCII.GetBytes(codeVerifier);
            var hash = sha256.ComputeHash(bytes);

            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static async Task<OAuth2Token> ExchangeCodeForTokenAsync(string code, OAuth2Options options)
        {
            using var client = new HttpClient();

            // GitHub requiere aceptar "application/json" para que devuelva JSON, o es form-urlencoded por defecto
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = options.RedirectUri,
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret
            };

            var requestContent = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(options.TokenEndpoint, requestContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error en intercambio de token: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();

            OAuth2Token token;

            if (options.TokenResponseFormat == OAuth2TokenResponseFormat.Json)
            {
                // Si la respuesta es JSON (no es el caso típico de GitHub salvo que pidas explícitamente)
                token = JsonSerializer.Deserialize<OAuth2Token>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            else
            {
                // GitHub responde por defecto en form-url-encoded
                var queryParams = System.Web.HttpUtility.ParseQueryString(content);
                token = new OAuth2Token
                {
                    AccessToken = queryParams["access_token"] ?? "",
                    TokenType = queryParams["token_type"] ?? "",
                    Scope = queryParams["scope"] ?? ""
                };
            }

            if (string.IsNullOrEmpty(token.AccessToken))
                throw new Exception("Token de acceso no recibido.");

            return token;
        }
    }
}
