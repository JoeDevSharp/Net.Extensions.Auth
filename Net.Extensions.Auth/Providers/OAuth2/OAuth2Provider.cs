using Net.Extensions.Auth.Core;
using Net.Extensions.Auth.Interfaces;

namespace Net.Extensions.Auth.Providers.OAuth2
{
    public class OAuth2Provider : IAuthProvider
    {
        private readonly OAuth2Options _options;
        private OAuth2Token? _token;
        private AuthUser? _user;

        public OAuth2Provider(OAuth2Options options)
        {
            _options = options;
        }

        public bool IsAuthenticated => _token != null;
        public AuthUser? CurrentUser => _user;

        public async Task<AuthUser?> LoginAsync()
        {
            var clientId = _options.ClientId;
            var redirectUri = _options.RedirectUri;
            var scope = string.Join(" ", _options.Scopes); ;

            var authUrl =
                "https://accounts.google.com/o/oauth2/v2/auth" +
                "?response_type=code" +
                $"&client_id={Uri.EscapeDataString(clientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&access_type=offline" +
                $"&prompt=consent";

            var code = await OAuth2LoginHelper.GetCodeViaBrowserAsync(authUrl, _options.RedirectUri);
            _token = await OAuth2LoginHelper.ExchangeCodeForTokenAsync(code, _options);

            _user = await OAuth2LoginHelper.GetUserInfoAsync(_token.AccessToken);

            return _user;
        }

        public Task LogoutAsync()
        {
            _token = null;
            _user = null;
            return Task.CompletedTask;
        }
    }

}
