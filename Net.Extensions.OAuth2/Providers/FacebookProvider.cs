using Net.Extensions.OAuth2.Abstracts;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class FacebookProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        private readonly OAuth2Options _options = new();
        private OAuth2Token? _token;
        private AuthUser? _user;

        private static readonly string[] DefaultScopes = new[] { "email", "public_profile" };

        public FacebookProvider(string clientId, string clientSecret, string redirectUri, string[]? scopes = null)
        {
            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                AuthorizationEndpoint = "https://www.facebook.com/v16.0/dialog/oauth",
                TokenEndpoint = "https://graph.facebook.com/v16.0/oauth/access_token",
                UserInfoEndpoint = "https://graph.facebook.com/me?fields=id,name,email,picture",
                Scopes = scopes?.Length > 0 ? scopes : DefaultScopes,
            };
        }
        public OAuth2Token? Token => _token;

        public async Task<AuthUser?> LoginAsync()
        {
            var clientId = _options.ClientId;
            var redirectUri = _options.RedirectUri;
            var scope = string.Join(",", _options.Scopes); // Facebook usa comas

            var authUrl =
                $"{_options.AuthorizationEndpoint}" +
                "?response_type=code" +
                $"&client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}";

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, redirectUri);

            _token = await OAuth2Helper.ExchangeCodeForTokenAsync(code, _options);

            _user = await GetUserInfoAsync(_token.AccessToken);

            return _user;
        }

        public static async Task<AuthUser> GetUserInfoAsync(string accessToken, string userInfoUrl = "")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var url = string.IsNullOrWhiteSpace(userInfoUrl)
                ? "https://graph.facebook.com/me?fields=id,name,email,picture"
                : userInfoUrl;

            var json = await client.GetStringAsync(url);
            var data = JsonDocument.Parse(json).RootElement;

            var pictureUrl = "";
            if (data.TryGetProperty("picture", out var pictureProp) &&
                pictureProp.TryGetProperty("data", out var dataProp) &&
                dataProp.TryGetProperty("url", out var urlProp))
            {
                pictureUrl = urlProp.GetString() ?? "";
            }

            return new AuthUser
            {
                Id = data.GetProperty("id").GetString() ?? "",
                Username = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Email = data.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                Picture = pictureUrl,
                Roles = new List<string>(), // Facebook no tiene roles estándar en OAuth
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
