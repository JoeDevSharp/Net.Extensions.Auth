using Net.Extensions.OAuth2.Enums;
using Net.Extensions.OAuth2.Abstracts;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class GithubProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        AuthUser? IAuthProvider.CurrentUser => throw new NotImplementedException();

        private readonly OAuth2Options _options = new();
        private OAuth2Token? _token;
        private AuthUser? _user;
        private static readonly string[] DefaultScopes = new[] { "read:user", "user:email" };

        public GithubProvider(string clientId, string clientSecret, string redirectUri, string[]? scopes = null)
        {
            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                UserInfoEndpoint = "https://api.github.com/user",
                Scopes = scopes?.Length > 0 ? scopes : DefaultScopes,
            };
        }

        public async Task<AuthUser?> LoginAsync()
        {
            var clientId = _options.ClientId;
            var redirectUri = _options.RedirectUri;
            var scope = string.Join(" ", _options.Scopes);

            var authUrl =
                $"{_options.AuthorizationEndpoint}" +
                "?response_type=code" +
                $"&client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}";

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, _options.RedirectUri);

            _token = await OAuth2Helper.ExchangeCodeForTokenAsync(code, _options);
            _user = await GetUserInfoAsync(_token.AccessToken, "https://api.github.com/user");

            return _user;
        }

        public static async Task<AuthUser> GetUserInfoAsync(string accessToken, string userInfoUrl = "")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Net.Extensions.OAuth2"); // requerido por GitHub

            var url = string.IsNullOrWhiteSpace(userInfoUrl)
                ? "https://api.github.com/user"
                : userInfoUrl;

            var json = await client.GetStringAsync(url);
            var data = JsonDocument.Parse(json).RootElement;

            // Email puede ser null si es privado; lo buscamos por separado
            string email = data.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : "";

            if (string.IsNullOrEmpty(email))
            {
                // Buscamos el email primario público
                var emailsJson = await client.GetStringAsync("https://api.github.com/user/emails");
                var emails = JsonDocument.Parse(emailsJson).RootElement;

                foreach (var entry in emails.EnumerateArray())
                {
                    if (entry.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                    {
                        email = entry.GetProperty("email").GetString() ?? "";
                        break;
                    }
                }
            }

            return new AuthUser
            {
                Id = data.GetProperty("id").GetRawText(), // GitHub ID es numérico
                Username = data.TryGetProperty("login", out var login) ? login.GetString() ?? "" : "",
                Email = email,
                Picture = data.TryGetProperty("avatar_url", out var avatar) ? avatar.GetString() ?? "" : "",
                Roles = new List<string>(), // GitHub no usa roles
                Claims = data.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString())
            };
        }

        public Task LogoutAsync()
        {
            _token = null;
            _user = null;
            return Task.CompletedTask;
        }
    }
}
