using SecureHR.Infrastructure.Cryptography;
using System.Security.Cryptography;

namespace SecureHR.Tests.Infrastructure
{
    public class PasswordHashingTests
    {
        [Fact]
        public void Correct_password_verifies()
        {
            var hash = PasswordHashing.Hash("CorrectHorse1!");

            Assert.Equal(PasswordCheck.Success, PasswordHashing.Verify(hash, "CorrectHorse1!"));
        }

        [Fact]
        public void Wrong_password_fails()
        {
            var hash = PasswordHashing.Hash("CorrectHorse1!");

            Assert.Equal(PasswordCheck.Failed, PasswordHashing.Verify(hash, "correcthorse1!"));
        }

        [Fact]
        public void Hashes_are_salted()
        {
            Assert.NotEqual(PasswordHashing.Hash("same"), PasswordHashing.Hash("same"));
        }

        [Fact]
        public void Legacy_hash_verifies_and_is_flagged_for_upgrade()
        {
            // Format written by earlier builds: Base64(salt):Base64(PBKDF2-SHA256, 10,000 iterations, 20 bytes)
            var salt = RandomNumberGenerator.GetBytes(16);
            var derived = Rfc2898DeriveBytes.Pbkdf2("OldPassword1!", salt, 10000, HashAlgorithmName.SHA256, 20);
            var legacy = $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(derived)}";

            Assert.Equal(PasswordCheck.SuccessRehashNeeded, PasswordHashing.Verify(legacy, "OldPassword1!"));
            Assert.Equal(PasswordCheck.Failed, PasswordHashing.Verify(legacy, "WrongPassword1!"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("garbage")]
        [InlineData("not:base64")]
        public void Malformed_hashes_fail_without_throwing(string? hash)
        {
            Assert.Equal(PasswordCheck.Failed, PasswordHashing.Verify(hash, "anything"));
        }
    }
}
