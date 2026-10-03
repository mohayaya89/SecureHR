using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public record EmployeeListItemDto
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public bool IsActive { get; set; }
    }
    
}
