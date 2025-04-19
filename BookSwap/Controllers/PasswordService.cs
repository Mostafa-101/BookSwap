using BCrypt.Net;
using System.Security.Cryptography;

namespace BookSwap.Controllers
{
    public static class PasswordService
    {
        private const int WorkFactor = 12;

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
    }
}