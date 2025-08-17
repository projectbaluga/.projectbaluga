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
        private const string InitialPasswordEnvVar = "PROJECTBALUGA_INITIAL_PASSWORD";
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        static PasswordStore()
        {
            var dir = Path.GetDirectoryName(StorePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(StorePath))
            {
                var initialPassword = Environment.GetEnvironmentVariable(InitialPasswordEnvVar);
                if (!string.IsNullOrWhiteSpace(initialPassword))
                {
                    SetPassword(initialPassword);
                }
            }
        }

        public static bool IsPasswordSet => File.Exists(StorePath);

        public static void SetPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = derive.GetBytes(HashSize);
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
            var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = derive.GetBytes(HashSize);
            return FixedTimeEquals(hash, storedHash);
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
