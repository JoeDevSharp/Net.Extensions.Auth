using System.Text;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Utils
{
    public static class JwtHelper
    {
        public static Dictionary<string, string> DecodeJwtPayload(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
                return new Dictionary<string, string>();

            var parts = jwt.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException("JWT no tiene el formato correcto.");

            var payload = parts[1];
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(bytes);

            var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ToString() ?? "";
            }
            return dict;
        }
    }
}
