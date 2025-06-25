namespace Net.Extensions.Auth.Providers.Jwt
{
    using Microsoft.IdentityModel.Tokens;
    using Net.Extensions.Auth.Core;
    using Net.Extensions.Auth.Exceptions;
    using Net.Extensions.Auth.Interfaces;
    using Net.Extensions.Auth.Utils;
    using System.IdentityModel.Tokens.Jwt;

    public class JwtTokenProvider : IAuthProvider
    {
        private readonly JwtOptions _options;
        private AuthUser? _user;

        public JwtTokenProvider(JwtOptions options)
        {
            _options = options;
            ParseToken(_options.Token);
        }

        public bool IsAuthenticated => _user != null;
        public AuthUser? CurrentUser => _user;

        public Task<AuthUser?> LoginAsync()
        {
            // Ya parseado en constructor
            return Task.FromResult(_user);
        }

        public Task LogoutAsync()
        {
            _user = null;
            return Task.CompletedTask;
        }

        private void ParseToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var handler = new JwtSecurityTokenHandler();

            var rsaKey = RsaKeyUtils.GetRsaPublicKeyFromPem(_options.AuthorityPublicKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = rsaKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = _options.ValidateExpiration,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            try
            {
                SecurityToken validatedToken;
                var principal = handler.ValidateToken(token, validationParameters, out validatedToken);

                _user = new AuthUser
                {
                    Id = principal.FindFirst("sub")?.Value ?? "",
                    Username = principal.FindFirst("preferred_username")?.Value
                               ?? principal.FindFirst("name")?.Value
                               ?? "Unknown",
                    Email = principal.FindFirst("email")?.Value ?? "",
                    Roles = principal.FindAll("role").Select(c => c.Value).ToList(),
                    Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value)
                };
            }
            catch (SecurityTokenException ex)
            {
                throw new AuthException("Token JWT inválido o firma incorrecta.", ex);
            }
        }

        public void ValidateJwtToken(string token, string publicKeyPem)
        {
            var rsaKey = RsaKeyUtils.GetRsaPublicKeyFromPem(publicKeyPem);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = rsaKey,
                ValidateIssuer = false,  // Cambia según tu caso
                ValidateAudience = false, // Cambia según tu caso
                ValidateLifetime = true,  // Validar expiración
                ClockSkew = TimeSpan.FromMinutes(2) // Margen por desfase de reloj
            };

            var handler = new JwtSecurityTokenHandler();

            try
            {
                SecurityToken validatedToken;
                var principal = handler.ValidateToken(token, validationParameters, out validatedToken);

                // Aquí tienes el token validado y claims
                Console.WriteLine("Token válido");
                foreach (var claim in principal.Claims)
                {
                    Console.WriteLine($"{claim.Type}: {claim.Value}");
                }
            }
            catch (SecurityTokenException ex)
            {
                Console.WriteLine($"Token inválido: {ex.Message}");
            }
        }
    }
}
