namespace Net.Extensions.Auth.Core
{
    public class AuthUser
    {
        public string Id { get; init; }
        public string Username { get; init; }
        public string Email { get; init; }
        public IReadOnlyList<string> Roles { get; init; }
        public IReadOnlyDictionary<string, string> Claims { get; init; }
    }
}
