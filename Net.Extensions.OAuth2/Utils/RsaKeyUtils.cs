using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Net.Extensions.OAuth2.Utils
{
    public static class RsaKeyUtils
    {
        public static RsaSecurityKey GetRsaPublicKeyFromPem(string publicKeyPem)
        {
            var publicKey = publicKeyPem
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "");

            var keyBytes = Convert.FromBase64String(publicKey);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return new RsaSecurityKey(rsa);
        }
    }
}
