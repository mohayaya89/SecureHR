using SecureHR.Domain.Entities;

namespace SecureHR.Domain.Interfaces.Repositories
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <param name="sortBy">"name" (default), "department" or "hireDate".</param>
        Task<(IEnumerable<Employee> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? sortBy = null,
            bool descending = false,
            CancellationToken ct = default);

        Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken ct = default);

        Task<IEnumerable<Employee>> GetActiveByDepartmentAsync(int departmentId, CancellationToken ct = default);

        Task<int> CountActiveInDepartmentAsync(int departmentId, CancellationToken ct = default);
    }
}
