namespace Net.Extensions.OAuth2.OAuth2
{
    public class OAuth2Token
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}
