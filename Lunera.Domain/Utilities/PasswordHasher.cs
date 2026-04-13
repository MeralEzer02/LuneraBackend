using System.Security.Cryptography;
using System.Text;

namespace Lunera.Domain.Utilities;
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Şifre doğrulama metodu (Login için gerekli olacak)
        public static bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            var hashOfInput = HashPassword(inputPassword);
            return hashOfInput == hashedPassword;
        }
    }