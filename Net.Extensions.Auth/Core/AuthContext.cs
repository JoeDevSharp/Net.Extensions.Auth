using Net.Extensions.Auth.Interfaces;

namespace Net.Extensions.Auth.Core
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
