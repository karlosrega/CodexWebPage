using System;
using System.Security.Cryptography;

namespace A1IntegrationsAdmin.Core
{
    /// <summary>Versioned PBKDF2-SHA256 hashes. Random salts and constant-time comparison.</summary>
    public static class PasswordHasher
    {
        public const int Iterations = 600000;

        public static string Hash(string password)
        {
            Validate(password);
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                return "pbkdf2-sha256$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(derive.GetBytes(32));
        }

        public static bool Verify(string password, string stored)
        {
            if (password == null || password.Length > 128 || stored == null)
                return false;
            try
            {
                var parts = stored.Split('$');
                int rounds;
                if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out rounds) || rounds < 100000 || rounds > 2000000)
                    return false;
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                if (salt.Length != 16 || expected.Length != 32)
                    return false;
                using (var derive = new Rfc2898DeriveBytes(password, salt, rounds, HashAlgorithmName.SHA256))
                {
                    var actual = derive.GetBytes(expected.Length);
                    int difference = 0;
                    for (int i = 0; i < actual.Length; i++)
                        difference |= actual[i] ^ expected[i];
                    return difference == 0;
                }
            }
            catch (FormatException) { return false; }
        }

        public static void Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
                throw new RuleException("La contraseña debe tener entre 12 y 128 caracteres.");
        }

        public static string Token()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static string TokenHash(string token)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token)));
        }
    }
}
