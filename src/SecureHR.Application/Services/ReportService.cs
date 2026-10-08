using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Domain.Interfaces.Repositories;

namespace SecureHR.Application.Services
{
    public class ReportService(
        IPayrollRecordRepository payrollRepository,
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository) : IReportService
    {
        public async Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default)
        {
            var records = await payrollRepository.GetByPeriodAsync(periodStart, periodEnd, ct);
            var recordList = records.ToList();

            return new PayrollSummaryReportDto
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TotalHeadcount = recordList.Select(r => r.EmployeeId).Distinct().Count(),
                TotalGross = recordList.Sum(r => r.GrossSalary),
                TotalNet = recordList.Sum(r => r.NetSalary),
                Breakdown = (await payrollRepository.GetDepartmentSummaryAsync(periodStart, periodEnd, ct)).ToList()
            };
        }

        public async Task<IEnumerable<DepartmentPayrollSummaryDto>> GetDepartmentPayrollSummaryAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default)
        {
            return await payrollRepository.GetDepartmentSummaryAsync(periodStart, periodEnd, ct);
        }

        public async Task<IEnumerable<HeadcountByDepartmentDto>> GetHeadcountReportAsync(
            CancellationToken ct = default)
        {
            return await departmentRepository.GetHeadcountSummaryAsync(ct);
        }

        public async Task<IEnumerable<EmployeeListItemDto>> GetEmployeesByDepartmentAsync(
            int departmentId, 
            CancellationToken ct = default)
        {
            var employees = await employeeRepository.GetActiveByDepartmentAsync(departmentId, ct);
            return employees
                .Select(e => new EmployeeListItemDto
                {
                    Id = e.Id,
                    FullName = $"{e.FirstName} {e.LastName}",
                    DepartmentName = e.Department?.Name ?? string.Empty,
                    HireDate = e.HireDate,
                    IsActive = e.IsActive
                })
                .ToList();
        }

        public async Task<IEnumerable<SalaryDistributionPointDto>> GetSalaryDistributionAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default)
        {
            var records = await payrollRepository.GetByPeriodAsync(periodStart, periodEnd, ct);

            // Buckets of gross pay per payroll record (i.e. monthly pay)
            const decimal salaryBucketSize = 1000m;
            var distribution = records
                .GroupBy(r => (int)(r.GrossSalary / salaryBucketSize))
                .OrderBy(g => g.Key) // numeric order; the bucket labels don't sort correctly as text
                .Select(g => new SalaryDistributionPointDto
                {
                    Bucket = $"${g.Key * salaryBucketSize:F0}-${((g.Key + 1) * salaryBucketSize) - 1:F0}",
                    Count = g.Count()
                })
                .ToList();

            return distribution;
        }
    }
}
