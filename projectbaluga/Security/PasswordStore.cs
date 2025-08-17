using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace projectbaluga.Security
{
    public static class PasswordStore
    {
        private static readonly string StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "projectbaluga", "admin.pass");
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        static PasswordStore()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            if (!File.Exists(StorePath))
            {
                SetPassword("amiralakbar");
            }
        }

        public static void SetPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            var payload = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
            var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(payload), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(StorePath, protectedBytes);
        }

        public static bool VerifyPassword(string password)
        {
            if (!File.Exists(StorePath)) return false;
            var protectedBytes = File.ReadAllBytes(StorePath);
            var payloadBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var payload = Encoding.UTF8.GetString(payloadBytes);
            var parts = payload.Split(':');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }
    }
}
