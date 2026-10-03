using SecureHR.Contracts;
using SecureHR.Web.Services;

namespace SecureHR.Web.Client
{
    public class AuditLogApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
        : ApiClientBase(httpClient, tokenProvider)
    {
        public Task<PagedResult<AuditLogDto>?> GetPagedAsync(AuditLogFilterRequestDto filter, int page, int pageSize)
        {
            var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
            if (filter.From is { } from) query.Add($"from={from:yyyy-MM-dd}");
            if (filter.To is { } to) query.Add($"to={to:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(filter.EntityName)) query.Add($"entityName={Uri.EscapeDataString(filter.EntityName)}");
            if (filter.EntityId is { } entityId) query.Add($"entityId={entityId}");
            if (!string.IsNullOrWhiteSpace(filter.Username)) query.Add($"username={Uri.EscapeDataString(filter.Username.Trim())}");
            if (filter.Action is { } action) query.Add($"action={action}");

            return GetAsync<PagedResult<AuditLogDto>>($"api/auditlog?{string.Join("&", query)}");
        }
    }
}
