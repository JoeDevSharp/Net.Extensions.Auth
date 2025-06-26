using Net.Extensions.OAuth2.Enums;
using Net.Extensions.OAuth2.Interfaces;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class MicrosoftProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        private readonly OAuth2Options _options = new();
        private OAuth2Token? _token;
        private AuthUser? _user;

        private static readonly string[] DefaultScopes = new[] { "openid", "email", "profile", "User.Read" };

        public MicrosoftProvider(string clientId, string clientSecret, string redirectUri, string[]? scopes = null)
        {
            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
                TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                UserInfoEndpoint = "https://graph.microsoft.com/oidc/userinfo",
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
                $"&scope={Uri.EscapeDataString(scope)}" +
                "&response_mode=query";

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, _options.RedirectUri);

            _token = await OAuth2Helper.ExchangeCodeForTokenAsync(code, _options);
            _user = await GetUserInfoAsync(_token.AccessToken, _options.UserInfoEndpoint);

            return _user;
        }

        public static async Task<AuthUser> GetUserInfoAsync(string accessToken, string userInfoUrl = "")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var url = string.IsNullOrWhiteSpace(userInfoUrl)
                ? "https://graph.microsoft.com/oidc/userinfo"
                : userInfoUrl;

            var json = await client.GetStringAsync(url);
            var data = JsonDocument.Parse(json).RootElement;

            return new AuthUser
            {
                Id = data.GetProperty("sub").GetString() ?? "",
                Username = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Email = data.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                Picture = "", // Microsoft no devuelve avatar en este endpoint
                Roles = new List<string>(), // generalmente no incluye roles
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
