using SecureHR.Contracts;

namespace SecureHR.Application.Interfaces.Services
{
    public interface IPayrollService
    {
        Task<PayrollRecordDto?> GetPayrollByIdAsync(
            int id, 
            CancellationToken ct = default);

        Task<PagedResult<PayrollRecordDto>> GetPayrollByEmployeeAsync(
            int employeeId, 
            int page = 1, 
            int pageSize = 10, 
            CancellationToken ct = default);

        Task<IEnumerable<PayrollRecordDto>> GetPayrollByPeriodAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default);

        Task<PayrollRecordDto?> GetLatestPayrollAsync(
            int employeeId, 
            CancellationToken ct = default);
        /// <exception cref="ArgumentException">The period is not a single calendar month.</exception>
        /// <exception cref="Exceptions.ConflictException">Payroll for the period was already processed.</exception>
        Task<PayrollBatchDto> ProcessPayrollBatchAsync(
            ProcessPayrollRequestDto request, 
            CancellationToken ct = default);
    }
}
