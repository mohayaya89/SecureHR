using Microsoft.EntityFrameworkCore;
using SecureHR.Contracts;
using SecureHR.Application.Interfaces.Repositories;
using SecureHR.Domain.Entities;
using SecureHR.Infrastructure.Data;
using System.Linq.Expressions;

namespace SecureHR.Infrastructure.Repository
{
    public class AuditLogRepository(SecureHRDbContext context) : GenericRepository<AuditLog>(context), IAuditLogRepository
    {
        private static readonly Expression<Func<AuditLog, AuditLogDto>> ToDto = a => new AuditLogDto
        {
            Id = a.Id,
            UserId = a.UserId ?? string.Empty,
            Username = a.Username ?? a.UserId ?? string.Empty,
            EntityName = a.EntityName ?? string.Empty,
            EntityId = a.EntityId.ToString(),
            Action = a.Action.ToString(),
            Timestamp = a.Timestamp,
            ChangesJson = a.ChangesJson
        };

        public Task<PagedResult<AuditLogDto>> GetPagedAsync(
            AuditLogFilterRequestDto filter,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = DbSet.AsNoTracking();

            if (filter.From is { } from)
            {
                var fromTime = from.ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.Timestamp >= fromTime);
            }

            if (filter.To is { } to)
            {
                // Inclusive of the whole "To" day
                var beforeTime = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.Timestamp < beforeTime);
            }

            if (!string.IsNullOrWhiteSpace(filter.EntityName))
                query = query.Where(a => a.EntityName == filter.EntityName);

            if (filter.EntityId is { } entityId)
                query = query.Where(a => a.EntityId == entityId);

            if (!string.IsNullOrWhiteSpace(filter.Username))
                query = query.Where(a => a.Username == filter.Username);

            if (filter.Action is { } action)
                query = query.Where(a => a.Action == action);

            return ToPagedAsync(query, page, pageSize, ct);
        }

        public Task<PagedResult<AuditLogDto>> GetByUserIdAsync(
            string userId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            return ToPagedAsync(DbSet.AsNoTracking().Where(a => a.UserId == userId), page, pageSize, ct);
        }

        public Task<PagedResult<AuditLogDto>> GetByEntityAsync(
            string entityName,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            return ToPagedAsync(DbSet.AsNoTracking().Where(a => a.EntityName == entityName), page, pageSize, ct);
        }

        public async Task<IEnumerable<AuditLogDto>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct = default)
        {
            return await DbSet
                .AsNoTracking()
                .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
                .OrderByDescending(a => a.Timestamp)
                .Select(ToDto)
                .ToListAsync(ct);
        }

        private static async Task<PagedResult<AuditLogDto>> ToPagedAsync(
            IQueryable<AuditLog> query, int page, int pageSize, CancellationToken ct)
        {
            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDto)
                .ToListAsync(ct);

            return new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            };
        }
    }
}
