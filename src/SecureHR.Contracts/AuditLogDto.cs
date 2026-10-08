using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHR.Contracts
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;       // joined in for display, not stored on AuditLog itself
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;         // enum serialized as string
        public DateTime Timestamp { get; set; }
        public string? ChangesJson { get; set; }
    }
}
