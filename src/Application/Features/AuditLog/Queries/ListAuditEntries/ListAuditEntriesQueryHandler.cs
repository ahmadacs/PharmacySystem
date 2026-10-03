using System.Text.Json;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using Domain.Entities.Audit;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed class ListAuditEntriesQueryHandler : IRequestHandler<ListAuditEntriesQuery, Result<PagedList<AuditEntryDto>>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IRepository<AuditEntry> _audit;
    private readonly IUserManager _users;

    public ListAuditEntriesQueryHandler(
        IRepository<AuditEntry> audit,
        IUserManager users)
    {
        _audit = audit;
        _users = users;
    }

    public async Task<Result<PagedList<AuditEntryDto>>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var entity = request.Entity;
        var action = request.Action;
        var from = request.From;
        var to = request.To;
        System.Linq.Expressions.Expression<Func<AuditEntry, bool>> predicate =
            e => (search == null || e.EntityName.Contains(search))
                && (!action.HasValue || e.Action == action.Value)
                && (entity == null || e.EntityName == entity)
                && (!from.HasValue || e.ChangedAt >= from.Value)
                && (!to.HasValue || e.ChangedAt <= to.Value);

        var pagedEntries = request.SortBy?.ToLowerInvariant() switch
        {
            "entity" => await _audit.PagedAsync(e => e, predicate, e => e.EntityName, request.ToPagination(), cancellationToken),
            "action" => await _audit.PagedAsync(e => e, predicate, e => e.Action, request.ToPagination(), cancellationToken),

            _ => await _audit.PagedAsync(e => e, predicate, e => e.ChangedAt, request.ToPagination(), cancellationToken)
        };

        var userNames = await _users.GetDisplayNamesAsync(
            pagedEntries.Items.Select(e => e.ChangedBy)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList(),
            cancellationToken);

        var items = pagedEntries.Select(e => e.ToDto(
            e.ChangedBy.HasValue ? userNames.GetValueOrDefault(e.ChangedBy.Value) : null,
            DeserializeChanges(e.ChangesJson)));

        return Result<PagedList<AuditEntryDto>>.Success(items);
    }

    private static IReadOnlyList<AuditChangeDto> DeserializeChanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<AuditChangeDto>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
