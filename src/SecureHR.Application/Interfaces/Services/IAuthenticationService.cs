namespace SecureHR.Application.Interfaces.Services
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates user and returns JWT token
        /// </summary>
        Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
