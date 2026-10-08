namespace SecureHR.Contracts
{
    /// <summary>Result of running payroll for one period.</summary>
    public class PayrollBatchDto
    {
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public List<PayrollBatchLineDto> Lines { get; set; } = new();

        /// <summary>Active employees not paid in this run, with the reason.</summary>
        public List<string> SkippedEmployees { get; set; } = new();
    }
}
