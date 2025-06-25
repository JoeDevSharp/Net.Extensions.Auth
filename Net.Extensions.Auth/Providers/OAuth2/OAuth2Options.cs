namespace Net.Extensions.Auth.Providers.OAuth2
{
    public class OAuth2Options
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string RedirectUri { get; set; } = "";
        public string AuthorizationEndpoint { get; set; } = "";
        public string TokenEndpoint { get; set; } = "";
        public string UserInfoEndpoint { get; set; } = "";
        public string[] Scopes { get; set; } = Array.Empty<string>();
    }
}
