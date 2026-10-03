using SecureHR.Domain.Enums;

namespace SecureHR.Contracts
{
    /// <summary>Optional filters for the audit log; all are combined with AND.</summary>
    public class AuditLogFilterRequestDto
    {
        /// <summary>First day to include (UTC).</summary>
        public DateOnly? From { get; set; }

        /// <summary>Last day to include (UTC), inclusive.</summary>
        public DateOnly? To { get; set; }

        /// <summary>Exact entity name, e.g. "Employee".</summary>
        public string? EntityName { get; set; }

        public int? EntityId { get; set; }

        /// <summary>Exact username.</summary>
        public string? Username { get; set; }

        public AuditAction? Action { get; set; }
    }
}
