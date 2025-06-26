using Net.Extensions.OAuth2.Interfaces;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
    public class LinkedInProvider : IAuthProvider
    {
        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        private readonly OAuth2Options _options = new();
        private OAuth2Token? _token;
        private AuthUser? _user;

        private static readonly string[] DefaultScopes = new[] { "r_liteprofile", "r_emailaddress" };

        public LinkedInProvider(string clientId, string clientSecret, string redirectUri, string[]? scopes = null)
        {
            _options = new OAuth2Options
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = redirectUri,
                AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization",
                TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken",
                UserInfoEndpoint = "", // Usamos dos endpoints específicos
                Scopes = scopes?.Length > 0 ? scopes : DefaultScopes
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

            var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, redirectUri);

            _token = await OAuth2Helper.ExchangeCodeForTokenAsync(code, _options);

            _user = await GetUserInfoAsync(_token.AccessToken);

            return _user;
        }

        public static async Task<AuthUser> GetUserInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // 1. Perfil básico
            var profileJson = await client.GetStringAsync("https://api.linkedin.com/v2/me");
            var profile = JsonDocument.Parse(profileJson).RootElement;

            string id = profile.GetProperty("id").GetString() ?? "";
            string firstName = profile.GetProperty("localizedFirstName").GetString() ?? "";
            string lastName = profile.GetProperty("localizedLastName").GetString() ?? "";
            string fullName = $"{firstName} {lastName}";

            // 2. Email
            var emailJson = await client.GetStringAsync("https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~))");
            var emailRoot = JsonDocument.Parse(emailJson).RootElement;
            var email = emailRoot
                .GetProperty("elements")[0]
                .GetProperty("handle~")
                .GetProperty("emailAddress")
                .GetString() ?? "";

            return new AuthUser
            {
                Id = id,
                Username = fullName,
                Email = email,
                Picture = "", // LinkedIn ya no da URL directa sin permisos extra
                Roles = new List<string>(),
                Claims = new Dictionary<string, string>
                {
                    { "firstName", firstName },
                    { "lastName", lastName },
                    { "fullName", fullName },
                    { "email", email }
                }
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
