using Net.Extensions.OAuth2.Abstracts;
using Net.Extensions.OAuth2.Models;
using Net.Extensions.OAuth2.Utils;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class AppleProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        private readonly OAuth2Options _options = new();
        private OAuth2Token? _token;
        private AuthUser? _user;
        private readonly string _clientSecretJwt;

        private static readonly string[] DefaultScopes = new[] { "name", "email" };

        public AppleProvider(string clientId, string clientSecret, string redirectUri, string[]? scopes = null)
        {
            _clientSecretJwt = clientSecret;

            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize",
                TokenEndpoint = "https://appleid.apple.com/auth/token",
                UserInfoEndpoint = "", // Apple no tiene endpoint userinfo estándar
                Scopes = scopes?.Length > 0 ? scopes : DefaultScopes
            };
        }
        public OAuth2Token? Token => _token;

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
                "&response_mode=form_post";

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, redirectUri);

            _token = await ExchangeCodeForTokenAsync(code);

            _user = GetUserInfoFromIdToken(_token.IdToken);

            return _user;
        }

        private async Task<OAuth2Token> ExchangeCodeForTokenAsync(string code)
        {
            using var client = new HttpClient();

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _clientSecretJwt,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = _options.RedirectUri
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(_options.TokenEndpoint, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<OAuth2Token>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }

        public static AuthUser GetUserInfoFromIdToken(string idToken)
        {
            var payload = JwtHelper.DecodeJwtPayload(idToken);

            return new AuthUser
            {
                Id = payload.TryGetValue("sub", out var sub) ? sub : "",
                Email = payload.TryGetValue("email", out var email) ? email : "",
                Username = "", // Apple no da username
                Picture = "",
                Roles = new List<string>(),
                Claims = payload
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
