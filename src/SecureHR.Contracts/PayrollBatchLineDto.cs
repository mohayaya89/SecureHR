using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class PayrollBatchLineDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
    }
}
