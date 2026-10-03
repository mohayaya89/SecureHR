using Microsoft.EntityFrameworkCore;
using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Domain.Entities;
using SecureHR.Infrastructure.Data;

namespace SecureHR.Infrastructure.Repository
{
    public class PayrollRecordRepository(SecureHRDbContext context) : GenericRepository<PayrollRecord>(context), IPayrollRecordRepository
    {
        public async Task<PagedResult<PayrollRecordDto>> GetPagedByEmployeeAsync(
            int employeeId, 
            int page, 
            int pageSize, 
            CancellationToken ct = default)
        {
            var query = DbSet
                .Where(p => p.EmployeeId == employeeId)
                .Include(p => p.Employee)
                .OrderByDescending(p => p.PeriodEnd);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PayrollRecordDto
                {
                    Id = p.Id,
                    EmployeeId = p.EmployeeId,
                    EmployeeFullName = $"{p.Employee!.FirstName} {p.Employee.LastName}",
                    PeriodStart = p.PeriodStart,
                    PeriodEnd = p.PeriodEnd,
                    GrossSalary = p.GrossSalary,
                    NetSalary = p.NetSalary,
                    ProcessedAt = p.ProcessedAt
                })
                .ToListAsync(ct);

            return new PagedResult<PayrollRecordDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<PayrollRecordDto>> GetByPeriodAsync(
            DateOnly periodStart, 
            DateOnly periodEnd, 
            CancellationToken ct = default)
        {
            return await DbSet
                .Where(p => p.PeriodStart >= periodStart && p.PeriodEnd <= periodEnd)
                .Include(p => p.Employee)
                .OrderBy(p => p.Employee!.LastName)
                .Select(p => new PayrollRecordDto
                {
                    Id = p.Id,
                    EmployeeId = p.EmployeeId,
                    EmployeeFullName = $"{p.Employee!.FirstName} {p.Employee.LastName}",
                    PeriodStart = p.PeriodStart,
                    PeriodEnd = p.PeriodEnd,
                    GrossSalary = p.GrossSalary,
                    NetSalary = p.NetSalary,
                    ProcessedAt = p.ProcessedAt
                })
                .ToListAsync(ct);
        }

        public async Task<PayrollRecordDto?> GetLatestByEmployeeAsync(
            int employeeId, 
            CancellationToken ct = default)
        {
            return await DbSet
                .Where(p => p.EmployeeId == employeeId)
                .Include(p => p.Employee)
                .OrderByDescending(p => p.PeriodEnd)
                .Select(p => new PayrollRecordDto
                {
                    Id = p.Id,
                    EmployeeId = p.EmployeeId,
                    EmployeeFullName = $"{p.Employee!.FirstName} {p.Employee.LastName}",
                    PeriodStart = p.PeriodStart,
                    PeriodEnd = p.PeriodEnd,
                    GrossSalary = p.GrossSalary,
                    NetSalary = p.NetSalary,
                    ProcessedAt = p.ProcessedAt
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<DepartmentPayrollSummaryDto>> GetDepartmentSummaryAsync(
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken ct = default)
        {
            var departments = await context.Departments
                .Where(d => d.IsActive)
                .Select(d => new { d.Id, d.Name, Headcount = d.Employees.Count(e => e.IsActive) })
                .ToListAsync(ct);

            // SQLite can't aggregate decimals in SQL, so fetch only the needed columns
            // for the period and aggregate in memory.
            var rows = await DbSet
                .Where(p => p.PeriodStart >= periodStart && p.PeriodEnd <= periodEnd)
                .Select(p => new { p.Employee!.DepartmentId, p.GrossSalary })
                .ToListAsync(ct);

            var byDepartment = rows.ToLookup(r => r.DepartmentId, r => r.GrossSalary);

            return departments
                .Select(d =>
                {
                    var salaries = byDepartment[d.Id].ToList();
                    return new DepartmentPayrollSummaryDto
                    {
                        DepartmentName = d.Name,
                        Headcount = d.Headcount,
                        TotalGross = salaries.Sum(),
                        AverageSalary = salaries.Count > 0 ? salaries.Average() : 0
                    };
                })
                .OrderBy(d => d.DepartmentName)
                .ToList();
        }

        public Task<bool> ExistsForPeriodAsync(
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken ct = default)
        {
            return DbSet.AnyAsync(p => p.PeriodStart == periodStart && p.PeriodEnd == periodEnd, ct);
        }
    }
}
