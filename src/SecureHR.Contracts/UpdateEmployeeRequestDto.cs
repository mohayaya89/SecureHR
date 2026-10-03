using System.ComponentModel.DataAnnotations;

namespace SecureHR.Contracts
{
    public class UpdateEmployeeRequestDto
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Select a department.")]
        public int DepartmentId { get; set; }

        [Required]
        public DateOnly HireDate { get; set; }

        [Range(typeof(decimal), "0.01", "100000000", ErrorMessage = "Annual salary must be greater than zero.")]
        public decimal AnnualSalary { get; set; }

        public bool IsActive { get; set; }
    }
}
