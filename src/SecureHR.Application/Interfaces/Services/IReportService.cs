using SecureHR.Contracts;

namespace SecureHR.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default);

        Task<IEnumerable<DepartmentPayrollSummaryDto>> GetDepartmentPayrollSummaryAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default);

        Task<IEnumerable<HeadcountByDepartmentDto>> GetHeadcountReportAsync(
            CancellationToken ct = default);

        Task<IEnumerable<EmployeeListItemDto>> GetEmployeesByDepartmentAsync(
            int departmentId, 
            CancellationToken ct = default);

        Task<IEnumerable<SalaryDistributionPointDto>> GetSalaryDistributionAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default);
    }
}
