namespace SecureHR.Domain.Entities
{
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        public DateOnly HireDate { get; set; }
        public decimal AnnualSalary { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<PayrollRecord>? PayrollRecords { get; set; }

        /// <summary>Display number derived from the Id, e.g. EMP-0007 (not stored).</summary>
        public string EmployeeNumber => $"EMP-{Id:D4}";
    }
}
