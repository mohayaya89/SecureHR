using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class PayrollRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
