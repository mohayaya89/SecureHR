using Microsoft.AspNetCore.Identity;
using SecureHR.Domain.Entities;
using System.Security.Cryptography;

namespace SecureHR.Infrastructure.Cryptography
{
    public enum PasswordCheck
    {
        Failed,
        Success,
        SuccessRehashNeeded
    }

    /// <summary>
    /// Password hashing via ASP.NET Core Identity's PasswordHasher
    /// (PBKDF2-HMAC-SHA512, 100k iterations, random salt, constant-time comparison).
    /// Also verifies the legacy "salt:hash" format produced by earlier builds so
    /// existing users can still log in; those hashes are flagged for upgrade.
    /// </summary>
    public static class PasswordHashing
    {
        private static readonly PasswordHasher<User> Hasher = new();

        // Used to spend the same time verifying when a username doesn't exist,
        // so response timing doesn't reveal which usernames are valid.
        private static readonly Lazy<string> DummyHash =
            new(() => Hasher.HashPassword(new User(), Guid.NewGuid().ToString()));

        public static string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            return Hasher.HashPassword(new User(), password);
        }

        public static PasswordCheck Verify(string? hash, string password)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return PasswordCheck.Failed;

            if (hash.Contains(':'))
                return VerifyLegacy(hash, password) ? PasswordCheck.SuccessRehashNeeded : PasswordCheck.Failed;

            try
            {
                return Hasher.VerifyHashedPassword(new User(), hash, password) switch
                {
                    PasswordVerificationResult.Success => PasswordCheck.Success,
                    PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
                    _ => PasswordCheck.Failed
                };
            }
            catch (FormatException)
            {
                return PasswordCheck.Failed;
            }
        }

        public static void VerifyAgainstDummy(string password)
        {
            Hasher.VerifyHashedPassword(new User(), DummyHash.Value, password);
        }

        // Legacy format: Base64(salt) + ":" + Base64(PBKDF2-SHA256, 10,000 iterations, 20 bytes)
        private static bool VerifyLegacy(string hash, string password)
        {
            var parts = hash.Split(':');
            if (parts.Length != 2)
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[0]);
                var expected = Convert.FromBase64String(parts[1]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
