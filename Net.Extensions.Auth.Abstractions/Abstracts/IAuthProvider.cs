
using Net.Extensions.OAuth2.Models;

namespace Net.Extensions.OAuth2.Abstracts
{
    public interface IAuthProvider
    {
        Task<AuthUser?> LoginAsync();
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        AuthUser? CurrentUser { get; }
        public OAuth2Token? Token { get; }
    }
}
