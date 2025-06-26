using Net.Extensions.Auth.Core;

namespace Net.Extensions.Auth.Interfaces
{
    public interface IAuthProvider
    {
        Task<AuthUser?> LoginAsync();
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        AuthUser? CurrentUser { get; }
    }
}
