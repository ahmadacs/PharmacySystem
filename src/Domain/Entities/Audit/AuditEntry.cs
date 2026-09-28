namespace Domain.Entities.Audit;

using Domain.Common;
using Domain.Enums;

public class AuditEntry : IEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EntityName { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public string? ChangesJson { get; private set; }

    private AuditEntry() { }

    public AuditEntry(string entityName, Guid entityId, AuditAction action, Guid? changedBy, DateTime changedAt, string? changesJson)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
        ChangesJson = changesJson;
    }
}