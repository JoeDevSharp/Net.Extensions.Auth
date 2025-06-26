using Net.Extensions.OAuth2.Interfaces;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class GoogleProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        private readonly OAuth2Options _options = new();
        private OAuth2Token? _token;
        private AuthUser? _user;
        private static readonly string[] DefaultScopes = new[] { "openid", "email", "profile" };

        public GoogleProvider(string clientId, string clientSecret, string redirectUri, string[]? scopes = null)
        {
            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
                TokenEndpoint = "https://oauth2.googleapis.com/token",
                UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo",
                Scopes = scopes?.Length > 0 ? scopes : DefaultScopes
            };
        }
        
        public async Task<AuthUser?> LoginAsync()
        {
            var clientId = _options.ClientId;
            var redirectUri = _options.RedirectUri;
            var scope = string.Join(" ", _options.Scopes); ;

            var authUrl =
                $"{_options.AuthorizationEndpoint}" +
                "?response_type=code" +
                $"&client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&access_type=offline" +
                $"&prompt=consent";

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, _options.RedirectUri);

            _token = await OAuth2Helper.ExchangeCodeForTokenAsync(code, _options);
            _user = await GetUserInfoAsync(_token.AccessToken);

            return _user;
        }

        public static async Task<AuthUser> GetUserInfoAsync(string accessToken, string userInfoUrl = "")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Net.Extensions.OAuth2");

            var url = string.IsNullOrWhiteSpace(userInfoUrl)
                ? "https://openidconnect.googleapis.com/v1/userinfo"
                : userInfoUrl;

            var json = await client.GetStringAsync(url);
            var data = JsonDocument.Parse(json).RootElement;

            return new AuthUser
            {
                Id = data.GetProperty("sub").GetString() ?? "",
                Username = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Email = data.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                Picture = data.TryGetProperty("picture", out var pic) ? pic.GetString() ?? "" : "",
                Roles = new List<string>(), // OIDC de Google no incluye roles
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
