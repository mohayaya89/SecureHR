using Microsoft.EntityFrameworkCore;
using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Domain.Entities;
using SecureHR.Infrastructure.Data;

namespace SecureHR.Infrastructure.Repository
{
    public class DepartmentRepository(SecureHRDbContext context) : GenericRepository<Department>(context), IDepartmentRepository
    {
        // Employee counts are computed in SQL; employee rows are never loaded.
        private IQueryable<DepartmentDto> ActiveDepartmentsWithCounts() =>
            DbSet
                .Where(d => d.IsActive)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    IsActive = d.IsActive,
                    EmployeeCount = d.Employees.Count(e => e.IsActive)
                });

        public async Task<(IEnumerable<DepartmentListItemDto> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? sortBy = null,
            bool descending = false,
            CancellationToken ct = default)
        {
            var query = ActiveDepartmentsWithCounts();
            var total = await query.CountAsync(ct);

            var ordered = sortBy?.ToLowerInvariant() switch
            {
                "employeecount" => descending
                    ? query.OrderByDescending(d => d.EmployeeCount).ThenBy(d => d.Name)
                    : query.OrderBy(d => d.EmployeeCount).ThenBy(d => d.Name),
                _ => descending
                    ? query.OrderByDescending(d => d.Name)
                    : query.OrderBy(d => d.Name)
            };

            var items = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DepartmentListItemDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    EmployeeCount = d.EmployeeCount
                })
                .ToListAsync(ct);

            return (items, total);
        }

        public Task<DepartmentDto?> GetDtoByIdAsync(int departmentId, CancellationToken ct = default)
        {
            return DbSet
                .Where(d => d.Id == departmentId)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    IsActive = d.IsActive,
                    EmployeeCount = d.Employees.Count(e => e.IsActive)
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllWithEmployeeCountAsync(CancellationToken ct = default)
        {
            return await ActiveDepartmentsWithCounts()
                .OrderBy(d => d.Name)
                .ToListAsync(ct);
        }

        public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
        {
            var normalized = name.Trim().ToLower();
            var query = DbSet.Where(d => d.Name.ToLower() == normalized);

            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            return query.AnyAsync(ct);
        }

        public async Task<IEnumerable<HeadcountByDepartmentDto>> GetHeadcountSummaryAsync(CancellationToken ct = default)
        {
            return await ActiveDepartmentsWithCounts()
                .OrderBy(d => d.Name)
                .Select(d => new HeadcountByDepartmentDto
                {
                    DepartmentName = d.Name,
                    Count = d.EmployeeCount
                })
                .ToListAsync(ct);
        }
    }
}
