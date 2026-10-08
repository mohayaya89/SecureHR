using SecureHR.Contracts;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;

namespace SecureHR.Application.Interfaces.Repositories
{
    public interface IPayrollRecordRepository : IGenericRepository<PayrollRecord>
    {
        Task<PagedResult<PayrollRecordDto>> GetPagedByEmployeeAsync(
            int employeeId, 
            int page, 
            int pageSize, 
            CancellationToken ct = default);

        Task<IEnumerable<PayrollRecordDto>> GetByPeriodAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default);

        Task<PayrollRecordDto?> GetLatestByEmployeeAsync(
            int employeeId, 
            CancellationToken ct = default);

        /// <summary>
        /// Per active department: active headcount plus gross totals for payroll records in the period.
        /// </summary>
        Task<IEnumerable<DepartmentPayrollSummaryDto>> GetDepartmentSummaryAsync(
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken ct = default);

        Task<bool> ExistsForPeriodAsync(
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken ct = default);
    }
}
