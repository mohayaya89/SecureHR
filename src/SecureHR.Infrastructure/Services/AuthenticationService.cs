using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;
using SecureHR.Infrastructure.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecureHR.Infrastructure.Services
{
    public class AuthenticationService(
        IUserRepository userRepository,
        IConfiguration configuration) : IAuthenticationService
    {
        private const string InvalidCredentials = "Invalid username or password";

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Username and password are required"
                };
            }

            var user = await userRepository.GetByUsernameAsync(username, ct);

            if (user == null)
            {
                // Security: Don't reveal whether username exists, by message or by timing
                PasswordHashing.VerifyAgainstDummy(password);
                return new AuthenticationResult { Success = false, Message = InvalidCredentials };
            }

            var check = PasswordHashing.Verify(user.PasswordHash, password);
            if (check == PasswordCheck.Failed)
            {
                return new AuthenticationResult { Success = false, Message = InvalidCredentials };
            }

            if (check == PasswordCheck.SuccessRehashNeeded)
            {
                // Upgrade legacy or outdated hashes transparently on successful login
                user.PasswordHash = PasswordHashing.Hash(password);
                userRepository.Update(user);
                await userRepository.SaveChangesAsync(ct);
            }

            var expiresAt = DateTime.UtcNow.AddHours(GetExpirationHours());
            var token = GenerateJwtToken(user, expiresAt);

            return new AuthenticationResult
            {
                Success = true,
                Message = "Authentication successful",
                Token = token,
                Username = user.Username,
                Role = user.Role.ToString(),
                ExpiresAt = expiresAt
            };
        }

        private string GenerateJwtToken(User user, DateTime expiresAt)
        {
            var jwtSecret = configuration["Jwt:Secret"];

            // Security: Validate configuration
            if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("JWT Secret must be configured and at least 32 characters");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username ?? ""),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int GetExpirationHours() =>
            int.TryParse(configuration["Jwt:ExpirationHours"], out var hours) && hours > 0 ? hours : 8;
    }
}
