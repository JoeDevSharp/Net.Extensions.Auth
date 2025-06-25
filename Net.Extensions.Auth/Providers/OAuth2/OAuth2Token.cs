namespace Net.Extensions.Auth.Providers.OAuth2
{
    public class OAuth2Token
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}
