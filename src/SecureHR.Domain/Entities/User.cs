using SecureHR.Domain.Enums;

namespace SecureHR.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public UserRole Role { get; set; }
    }
}
