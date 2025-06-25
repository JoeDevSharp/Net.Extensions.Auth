namespace Net.Extensions.Auth.Providers.Jwt
{
    public class JwtOptions
    {
        public string Token { get; set; } = "";
        public string AuthorityPublicKey { get; set; } = ""; // Opcional: para validar firma
        public bool ValidateExpiration { get; set; } = true;
    }
}
