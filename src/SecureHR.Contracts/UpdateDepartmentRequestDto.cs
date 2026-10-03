using System.ComponentModel.DataAnnotations;

namespace SecureHR.Contracts
{
    public class UpdateDepartmentRequestDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
    }
}
