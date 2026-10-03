using SecureHR.Domain.Enums;

namespace SecureHR.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? EntityName { get; set; }
        public int EntityId { get; set; }
        public AuditAction Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ChangesJson { get; set; }    
    }
}
