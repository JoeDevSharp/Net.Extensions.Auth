using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Auth.Api.Services
{
    public class OAuth2Service
    {
        private readonly HttpClient _http;

        public OAuth2Service(HttpClient http) => _http = http;

        public async Task<string> ExchangeCodeForTokenAsync(string code, OAuth2Options options)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["redirect_uri"] = options.RedirectUri,
                ["grant_type"] = "authorization_code"
            });

            var response = await _http.PostAsync(options.TokenEndpoint, content);
            var json = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(json).RootElement;
            return doc.GetProperty("access_token").GetString()!;
        }

        public async Task<AuthUser> GetUserInfoAsync(string accessToken, string userInfoEndpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.UserAgent.ParseAdd("Auth.Api"); // GitHub requiere esto

            var json = await (await _http.SendAsync(request)).Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement;

            return new AuthUser
            {
                Id = data.TryGetProperty("sub", out var sub) ? sub.GetString() ?? "" :
                     data.TryGetProperty("id", out var id) ? id.ToString() : "",
                Username = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Email = data.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                Picture = data.TryGetProperty("picture", out var pic) ? pic.GetString() ?? "" :
                          data.TryGetProperty("avatar_url", out var avatar) ? avatar.GetString() ?? "" : ""
            };
        }
    }
}
