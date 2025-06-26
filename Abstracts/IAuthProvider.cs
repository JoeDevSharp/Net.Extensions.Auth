
using Net.Extensions.OAuth2.Models;

namespace Net.Extensions.OAuth2.Interfaces
{
    public interface IAuthProvider
    {
        Task<AuthUser?> LoginAsync();
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        AuthUser? CurrentUser { get; }
    }
}
