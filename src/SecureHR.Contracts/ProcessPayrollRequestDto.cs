using System.ComponentModel.DataAnnotations;

namespace SecureHR.Contracts
{
    /// <summary>
    /// Payroll runs one calendar month at a time: PeriodStart must be the 1st and
    /// PeriodEnd the last day of the same month.
    /// </summary>
    public class ProcessPayrollRequestDto
    {
        [Required]
        public DateOnly PeriodStart { get; set; }

        [Required]
        public DateOnly PeriodEnd { get; set; }
    }
}
