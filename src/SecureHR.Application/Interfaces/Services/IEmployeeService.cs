using SecureHR.Contracts;

namespace SecureHR.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <param name="sortBy">"name" (default), "department" or "hireDate".</param>
        Task<PagedResult<EmployeeListItemDto>> GetPagedAsync(
            int page, int pageSize, string? sortBy = null, bool descending = false, CancellationToken ct = default);

        /// <exception cref="ArgumentException">The department doesn't exist or is inactive.</exception>
        /// <exception cref="Exceptions.ConflictException">The email is already used.</exception>
        Task<EmployeeDto> CreateAsync(CreateEmployeeRequestDto request, string currentUserId, CancellationToken ct = default);

        /// <exception cref="KeyNotFoundException">No such employee.</exception>
        /// <exception cref="ArgumentException">The department doesn't exist or is inactive.</exception>
        /// <exception cref="Exceptions.ConflictException">The email is already used.</exception>
        Task UpdateAsync(int id, UpdateEmployeeRequestDto request, string currentUserId, CancellationToken ct = default);

        /// <exception cref="KeyNotFoundException">No such employee.</exception>
        Task DeactivateAsync(int id, string currentUserId, CancellationToken ct = default);
    }
}
