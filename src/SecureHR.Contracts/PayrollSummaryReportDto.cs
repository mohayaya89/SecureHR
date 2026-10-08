using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class PayrollSummaryReportDto
    {
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public int TotalHeadcount { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public List<DepartmentPayrollSummaryDto> Breakdown { get; set; } = new();
    }
}
