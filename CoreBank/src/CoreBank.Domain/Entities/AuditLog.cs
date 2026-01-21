using CoreBank.Domain.Common;

namespace CoreBank.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string userId,
        string action,
        string entityType,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };
    }
}
