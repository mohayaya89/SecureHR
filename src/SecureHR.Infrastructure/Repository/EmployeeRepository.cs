using Microsoft.EntityFrameworkCore;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;
using SecureHR.Infrastructure.Data;

namespace SecureHR.Infrastructure.Repository
{
    public class EmployeeRepository(SecureHRDbContext context) : GenericRepository<Employee>(context), IEmployeeRepository
    {
        public override Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return DbSet
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLower();
            var query = DbSet.Where(e => e.Email.ToLower() == normalized);

            if (excludeId.HasValue)
                query = query.Where(e => e.Id != excludeId.Value);

            return query.AnyAsync(ct);
        }

        public async Task<(IEnumerable<Employee> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? sortBy = null,
            bool descending = false,
            CancellationToken ct = default)
        {
            var query = DbSet.AsNoTracking().Include(e => e.Department).AsQueryable();
            var total = await query.CountAsync(ct);

            IOrderedQueryable<Employee> ordered = sortBy?.ToLowerInvariant() switch
            {
                "department" => descending
                    ? query.OrderByDescending(e => e.Department.Name).ThenBy(e => e.LastName)
                    : query.OrderBy(e => e.Department.Name).ThenBy(e => e.LastName),
                "hiredate" => descending
                    ? query.OrderByDescending(e => e.HireDate)
                    : query.OrderBy(e => e.HireDate),
                _ => descending
                    ? query.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName)
                    : query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            };

            var items = await ordered
                .ThenBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<IEnumerable<Employee>> GetActiveByDepartmentAsync(int departmentId, CancellationToken ct = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(e => e.Department)
                .Where(e => e.DepartmentId == departmentId && e.IsActive)
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .ToListAsync(ct);
        }

        public Task<int> CountActiveInDepartmentAsync(int departmentId, CancellationToken ct = default)
        {
            return DbSet.CountAsync(e => e.DepartmentId == departmentId && e.IsActive, ct);
        }
    }
}
