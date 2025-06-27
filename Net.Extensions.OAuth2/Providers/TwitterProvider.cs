using Net.Extensions.OAuth2.Abstracts;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class TwitterProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        private readonly OAuth2Options _options;
        private readonly string? _codeVerifier;
        private readonly string? _codeChallenge;
        private OAuth2Token? _token;
        private AuthUser? _user;

        private static readonly string[] DefaultScopes = new[]
        {
            "tweet.read", "users.read", "offline.access"
        };

        public TwitterProvider(string clientId, string redirectUri, string? clientSecret = null, string[]? scopes = null)
        {
            var scopesToUse = scopes?.Length > 0 ? scopes : DefaultScopes;

            // Si no hay clientSecret => flujo público (PKCE)
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                _codeVerifier = OAuth2Helper.GenerateCodeVerifier();
                _codeChallenge = OAuth2Helper.GenerateCodeChallenge(_codeVerifier);
            }

            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret ?? "",
                RedirectUri = redirectUri,
                AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize",
                TokenEndpoint = "https://api.twitter.com/2/oauth2/token",
                UserInfoEndpoint = "https://api.twitter.com/2/users/me",
                Scopes = scopesToUse
            };
        }

        public async Task<AuthUser?> LoginAsync()
        {
            var scope = Uri.EscapeDataString(string.Join(" ", _options.Scopes));
            var authUrl = $"{_options.AuthorizationEndpoint}?" +
                          $"response_type=code" +
                          $"&client_id={_options.ClientId}" +
                          $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                          $"&scope={scope}";

            if (_codeChallenge != null)
                authUrl += $"&code_challenge={_codeChallenge}&code_challenge_method=S256";

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, _options.RedirectUri);

            _token = await ExchangeCodeForTokenAsync(code);
            _user = await GetUserInfoAsync(_token.AccessToken);

            return _user;
        }

        private async Task<OAuth2Token> ExchangeCodeForTokenAsync(string code)
        {
            using var client = new HttpClient();
            var body = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _options.RedirectUri,
                ["client_id"] = _options.ClientId,
            };

            if (_codeVerifier != null)
            {
                // flujo público (PKCE)
                body["code_verifier"] = _codeVerifier;
            }
            else
            {
                // flujo confidencial (usa client_secret)
                var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            }

            var content = new FormUrlEncodedContent(body);
            var response = await client.PostAsync(_options.TokenEndpoint, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OAuth2Token>(json)!;
        }

        public static async Task<AuthUser> GetUserInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetStringAsync("https://api.twitter.com/2/users/me");
            var data = JsonDocument.Parse(response).RootElement;
            var user = data.GetProperty("data");

            return new AuthUser
            {
                Id = user.GetProperty("id").GetString() ?? "",
                Username = user.GetProperty("username").GetString() ?? "",
                Email = "", // Twitter no devuelve email por defecto
                Picture = "", // Tampoco la foto
                Roles = new List<string>(),
                Claims = user.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString())
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
