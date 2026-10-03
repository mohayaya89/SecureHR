using SecureHR.Contracts;

namespace SecureHR.Application.Interfaces.Services
{
    public interface IDepartmentService
    {
        Task<DepartmentDto?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <param name="sortBy">"name" (default) or "employeeCount".</param>
        Task<PagedResult<DepartmentListItemDto>> GetPagedAsync(
            int page, int pageSize, string? sortBy = null, bool descending = false, CancellationToken ct = default);

        /// <exception cref="Exceptions.ConflictException">The name is already used.</exception>
        Task<DepartmentDto> CreateAsync(UpdateDepartmentRequestDto request, CancellationToken ct = default);

        /// <exception cref="KeyNotFoundException">No such department.</exception>
        /// <exception cref="Exceptions.ConflictException">The name is already used.</exception>
        Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentRequestDto request, CancellationToken ct = default);

        /// <exception cref="KeyNotFoundException">No such department.</exception>
        /// <exception cref="Exceptions.ConflictException">The department still has active employees.</exception>
        Task DeactivateAsync(int id, CancellationToken ct = default);
    }
}
