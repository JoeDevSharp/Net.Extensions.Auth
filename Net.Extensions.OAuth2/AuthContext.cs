using Net.Extensions.OAuth2.Abstracts;
using Net.Extensions.OAuth2.Models;

namespace Net.Extensions.OAuth2
{
    public static class AuthContext
    {
        private static IAuthProvider? _provider;

        public static void RegisterProvider(IAuthProvider provider)
        {
            _provider = provider;
        }

        public static Task<AuthUser?> LoginAsync() =>
            _provider?.LoginAsync() ?? Task.FromResult<AuthUser?>(null);

        public static Task LogoutAsync() =>
            _provider?.LogoutAsync() ?? Task.CompletedTask;

        public static bool IsAuthenticated => _provider?.IsAuthenticated ?? false;
        public static AuthUser? CurrentUser => _provider?.CurrentUser;
    }
}
