using System.Net.Http.Headers;

namespace Net.Extensions.OAuth2.Extensions
{
    public static class HttpClientExtensions
    {
        public static void UseAuthBearer(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
