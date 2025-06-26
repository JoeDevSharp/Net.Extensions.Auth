using System;
using System.Text.Json.Serialization;

namespace Net.Extensions.OAuth2.Models
{
    public class OAuth2Token
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        // Expira en segundos a partir de la respuesta, calcular expiración absoluta
        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds { get; set; }

        // Token ID JWT (Google y otros)
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        // Calculado en base a la hora actual + expires_in
        [JsonIgnore]
        public DateTime ExpiresAt => _expiresAt ??= DateTime.UtcNow.AddSeconds(ExpiresInSeconds);

        private DateTime? _expiresAt;
    }
}
