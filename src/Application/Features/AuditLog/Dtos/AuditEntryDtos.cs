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