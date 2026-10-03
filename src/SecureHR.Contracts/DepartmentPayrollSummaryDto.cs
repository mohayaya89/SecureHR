using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class DepartmentPayrollSummaryDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Headcount { get; set; }
        public decimal TotalGross { get; set; }
        public decimal AverageSalary { get; set; }
    }
}
