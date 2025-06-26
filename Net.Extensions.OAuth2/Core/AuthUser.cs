namespace Net.Extensions.Auth.Core
{
    public class AuthUser
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Picture { get; set; } = ""; // ← nuevo campo opcional
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, string> Claims { get; set; } = new();
    }

}
