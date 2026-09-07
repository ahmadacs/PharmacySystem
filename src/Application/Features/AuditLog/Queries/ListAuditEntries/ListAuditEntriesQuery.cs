using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed record ListAuditEntriesQuery : PagedQuery, IRequest<Result<PagedList<AuditEntryDto>>>
{
    [EnumDataType(typeof(AuditAction), ErrorMessage = "Action must be Created, Updated or Deleted.")]
    public AuditAction? Action { get; init; }

    [StringLength(200, ErrorMessage = "Entity must be at most 200 characters.")]
    public string? Entity { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public override string? SortBy { get; init; } = "changedAt";
}
