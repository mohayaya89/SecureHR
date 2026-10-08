using System.ComponentModel.DataAnnotations;

namespace SecureHR.Contracts
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 255 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._@-]+$", ErrorMessage = "Username contains invalid characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(1000, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
