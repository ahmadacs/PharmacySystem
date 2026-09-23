using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
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

    private readonly IBaseRepository<AuditEntry> _audit;
    private readonly IAuditDisplayNameResolver _displays;

    public ListAuditEntriesQueryHandler(
        IBaseRepository<AuditEntry> audit,
        IAuditDisplayNameResolver displays)
    {
        _audit = audit;
        _displays = displays;
    }

    public async Task<Result<PagedList<AuditEntryDto>>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(200);

        var spec = new Specification<AuditEntry, AuditEntry>(e => e);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var trimmed = request.Search.Trim();
            spec.Where(e => e.EntityName.Contains(trimmed));
        }

        if (request.Action.HasValue)
            spec.Where(e => e.Action == request.Action.Value);

        if (!string.IsNullOrWhiteSpace(request.Entity))
            spec.Where(e => e.EntityName == request.Entity);

        if (request.From.HasValue)
            spec.Where(e => e.ChangedAt >= request.From.Value);

        if (request.To.HasValue)
            spec.Where(e => e.ChangedAt <= request.To.Value);

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "entity" => SortDir(e => e.EntityName, request.SortDir),
            "action" => SortDir(e => e.Action, request.SortDir),
            // No author column without the join: stable date order instead.
            _ => SortDir(e => e.ChangedAt, request.SortDir)
        });

        var totalCount = await _audit.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var entries = await _audit.ListAsync(spec, cancellationToken);

        var changesById = entries.ToDictionary(e => e.Id, e => DeserializeChanges(e.ChangesJson));

        // One batched pass: entry labels, FK value labels and user names
        // (author display names come from here now, keyed by ChangedBy).
        var resolved = await _displays.ResolvePageAsync(
            entries,
            CollectValueRefs(changesById.Values.SelectMany(c => c)),
            entries.Select(e => e.ChangedBy)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList(),
            cancellationToken);

        var items = entries
            .Select(e => e.ToDto(
                e.ChangedBy.HasValue ? resolved.UserNames.GetValueOrDefault(e.ChangedBy.Value) : null,
                AttachValueDisplays(changesById[e.Id], resolved.ValueDisplays),
                resolved.EntryDisplays.GetValueOrDefault(e.Id)))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<AuditEntryDto>>.Success(items);
    }

    private static Func<IQueryable<AuditEntry>, IOrderedQueryable<AuditEntry>> SortDir<TKey>(
        System.Linq.Expressions.Expression<Func<AuditEntry, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q => q.OrderByDescending(keySelector)
            : q => q.OrderBy(keySelector);

    /// <summary>Collects every GUID change value that may reference another entity.</summary>
    private static IReadOnlyCollection<AuditValueRef> CollectValueRefs(
        IEnumerable<AuditChangeDto> changes)
    {
        var refs = new HashSet<AuditValueRef>();
        foreach (var c in changes)
        {
            if (Guid.TryParse(c.OldValue, out var oldId))
                refs.Add(new AuditValueRef(c.Property, oldId));
            if (Guid.TryParse(c.NewValue, out var newId))
                refs.Add(new AuditValueRef(c.Property, newId));
        }
        return refs;
    }

    private static IReadOnlyList<AuditChangeDto> AttachValueDisplays(
        IReadOnlyList<AuditChangeDto> changes,
        IReadOnlyDictionary<AuditValueRef, string> displays)
    {
        if (displays.Count == 0)
            return changes;

        return changes
            .Select(c => c with
            {
                OldValueDisplay = c.OldValue is not null
                    && Guid.TryParse(c.OldValue, out var oldId)
                    && displays.TryGetValue(new AuditValueRef(c.Property, oldId), out var oldDisplay)
                    ? oldDisplay : c.OldValueDisplay,
                NewValueDisplay = c.NewValue is not null
                    && Guid.TryParse(c.NewValue, out var newId)
                    && displays.TryGetValue(new AuditValueRef(c.Property, newId), out var newDisplay)
                    ? newDisplay : c.NewValueDisplay,
            })
            .ToList();
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
