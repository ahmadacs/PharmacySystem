namespace Domain.Entities.Audit;

using Domain.Enums;

/// <summary>
/// A change-tracking record written automatically on every save: which entity was
/// changed, by whom, when, and the old/new values of the changed properties.
/// Deliberately NOT a BaseEntity — audit rows are immutable and never soft-deleted.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EntityName { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public DateTime ChangedAt { get; private set; }

    /// <summary>JSON array of { property, oldValue, newValue } for the changed properties.</summary>
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