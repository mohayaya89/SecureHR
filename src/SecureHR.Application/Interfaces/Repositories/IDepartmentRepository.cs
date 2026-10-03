using SecureHR.Contracts;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;

namespace SecureHR.Application.Interfaces.Repositories
{
    public interface IDepartmentRepository : IGenericRepository<Department>
    {
        /// <summary>Active departments with their active-employee counts.</summary>
        /// <param name="sortBy">"name" (default) or "employeeCount".</param>
        Task<(IEnumerable<DepartmentListItemDto> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? sortBy = null,
            bool descending = false,
            CancellationToken ct = default);

        Task<DepartmentDto?> GetDtoByIdAsync(
            int departmentId,
            CancellationToken ct = default);

        /// <summary>All active departments with their active-employee counts.</summary>
        Task<IEnumerable<DepartmentDto>> GetAllWithEmployeeCountAsync(
            CancellationToken ct = default);

        /// <summary>Case-insensitive name check across all departments.</summary>
        Task<bool> NameExistsAsync(
            string name,
            int? excludeId = null,
            CancellationToken ct = default);

        Task<IEnumerable<HeadcountByDepartmentDto>> GetHeadcountSummaryAsync(
            CancellationToken ct = default);
    }
}
