using Domain.Entities.Audit;

namespace Application.Features.AuditLog.Dtos;

public static class AuditMapping
{
    public static AuditEntryDto ToDto(
        this AuditEntry entry,
        string? authorName,
        IReadOnlyList<AuditChangeDto> changes)
        => new(
            entry.Id,
            entry.EntityName,
            entry.EntityId,
            entry.Action,
            entry.ChangedBy,
            authorName,
            entry.ChangedAt,
            changes);
}
