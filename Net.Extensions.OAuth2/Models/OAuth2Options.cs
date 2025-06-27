using Net.Extensions.OAuth2.Enums;

namespace Net.Extensions.OAuth2.Models
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
        public OAuth2TokenResponseFormat TokenResponseFormat { get; set; } = OAuth2TokenResponseFormat.Json;
    }
}
