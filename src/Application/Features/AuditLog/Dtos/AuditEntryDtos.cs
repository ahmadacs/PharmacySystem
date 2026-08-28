using Domain.Enums;

namespace Application.Features.AuditLog.Dtos;

public sealed record AuditChangeDto(string Property, string? OldValue, string? NewValue);

public sealed record AuditEntryDto(
    Guid Id,
    string EntityName,
    Guid EntityId,
    AuditAction Action,
    Guid? ChangedBy,
    string? ChangedByName,
    DateTime ChangedAt,
    IReadOnlyList<AuditChangeDto> Changes);