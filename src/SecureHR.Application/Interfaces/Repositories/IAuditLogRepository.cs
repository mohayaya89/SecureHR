using SecureHR.Contracts;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Interfaces.Repositories;

namespace SecureHR.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository : IGenericRepository<AuditLog>
    {
        Task<PagedResult<AuditLogDto>> GetPagedAsync(
            AuditLogFilterRequestDto filter,
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<PagedResult<AuditLogDto>> GetByUserIdAsync(
            string userId, 
            int page, 
            int pageSize, 
            CancellationToken ct = default);

        Task<PagedResult<AuditLogDto>> GetByEntityAsync(
            string entityName, 
            int page, 
            int pageSize, 
            CancellationToken ct = default);

        Task<IEnumerable<AuditLogDto>> GetByDateRangeAsync(
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken ct = default);
    }
}
