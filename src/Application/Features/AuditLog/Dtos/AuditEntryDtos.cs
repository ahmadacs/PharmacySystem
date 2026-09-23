using Domain.Entities.Audit;
using Domain.Enums;

namespace Application.Features.AuditLog.Dtos;

public sealed record AuditChangeDto(
    string Property,
    string? OldValue,
    string? NewValue,
    string? OldValueDisplay = null,
    string? NewValueDisplay = null);

public sealed record AuditEntryDto(
    Guid Id,
    string EntityName,
    Guid EntityId,
    string? EntityDisplay,
    AuditAction Action,
    Guid? ChangedBy,
    string? ChangedByName,
    DateTime ChangedAt,
    IReadOnlyList<AuditChangeDto> Changes);

public static class AuditMapping
{
    public static AuditEntryDto ToDto(
        this AuditEntry entry,
        string? authorName,
        IReadOnlyList<AuditChangeDto> changes,
        string? entityDisplay = null)
        => new(
            entry.Id,
            entry.EntityName,
            entry.EntityId,
            entityDisplay,
            entry.Action,
            entry.ChangedBy,
            authorName,
            entry.ChangedAt,
            changes);
}