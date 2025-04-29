using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BookSwap.Controllers
{
    public static class PasswordService
    {
        private const int WorkFactor = 12;
        private static readonly byte[] AesKey;

        static PasswordService()
        {
            var base64Key = "yB7WfM0RZqgF6k5Jt1Q3aVjPd+WmUlR2nLOcYfZK+M0=";
            AesKey = Convert.FromBase64String(base64Key);
            if (AesKey.Length != 32)
                throw new InvalidOperationException("AES key must be 32 bytes long.");
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public static bool VerifyPassword(string inputPassword, string storedPasswordHash)
        {
            if (string.IsNullOrEmpty(inputPassword))
            {
                throw new ArgumentNullException(nameof(inputPassword));
            }

            if (string.IsNullOrEmpty(storedPasswordHash))
            {
                throw new ArgumentNullException(nameof(storedPasswordHash));
            }

            return BCrypt.Net.BCrypt.Verify(inputPassword, storedPasswordHash);
        }

        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = AesKey;
                    aes.GenerateIV();
                    var iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(iv, 0, iv.Length);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Encryption failed.", ex);
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = AesKey;
                    byte[] iv = new byte[16];
                    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                    using (var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Decryption failed.", ex);
            }
        }

        public static string GenerateJwtToken(string secretKey, string issuer, string audience,
            string userName, string role, string? userId = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim("name", userName),
                new Claim("role", role)
            };

            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim($"{role.ToLower()}Id", userId));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = issuer,
                Audience = audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}