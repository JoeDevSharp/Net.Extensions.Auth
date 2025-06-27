using Net.Extensions.OAuth2.Abstracts;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class KeycloakProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        private readonly OAuth2Options _options = new();
        private OAuth2Token? _token;
        private AuthUser? _user;
        private static readonly string[] DefaultScopes = new[] { "openid", "profile", "email" };

        public KeycloakProvider(string baseUrl, string realm, string clientId, string clientSecret, string redirectUri, string[]? scopes = null)
        {
            var realmUrl = $"{baseUrl.TrimEnd('/')}/realms/{realm}";

            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                AuthorizationEndpoint = $"{realmUrl}/protocol/openid-connect/auth",
                TokenEndpoint = $"{realmUrl}/protocol/openid-connect/token",
                UserInfoEndpoint = $"{realmUrl}/protocol/openid-connect/userinfo",
                Scopes = scopes?.Length > 0 ? scopes : DefaultScopes
            };
        }
        public OAuth2Token? Token => _token;

        public async Task<AuthUser?> LoginAsync()
        {
            var authUrl =
                $"{_options.AuthorizationEndpoint}" +
                "?response_type=code" +
                $"&client_id={Uri.EscapeDataString(_options.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                $"&scope={Uri.EscapeDataString(string.Join(" ", _options.Scopes))}";

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, _options.RedirectUri);

            _token = await OAuth2Helper.ExchangeCodeForTokenAsync(code, _options);
            _user = await GetUserInfoAsync(_token.AccessToken, _options.UserInfoEndpoint);

            return _user;
        }

        public static async Task<AuthUser> GetUserInfoAsync(string accessToken, string userInfoUrl)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var json = await client.GetStringAsync(userInfoUrl);
            var data = JsonDocument.Parse(json).RootElement;

            return new AuthUser
            {
                Id = data.GetProperty("sub").GetString() ?? "",
                Username = data.TryGetProperty("preferred_username", out var username) ? username.GetString() ?? "" : "",
                Email = data.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                Picture = data.TryGetProperty("picture", out var pic) ? pic.GetString() ?? "" : "",
                Roles = new List<string>(), // Puedes extraer roles del token si configuras scope 'roles'
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
