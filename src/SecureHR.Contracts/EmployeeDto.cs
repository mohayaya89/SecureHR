using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }

        /// <summary>Only returned to Admin and HR; null for other roles.</summary>
        public decimal? AnnualSalary { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
