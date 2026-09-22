using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed class ListAuditEntriesQueryHandler : IRequestHandler<ListAuditEntriesQuery, Result<PagedList<AuditEntryDto>>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAuditRepository _audit;
    private readonly IAuditDisplayNameResolver _displays;

    public ListAuditEntriesQueryHandler(
        IAuditRepository audit,
        IAuditDisplayNameResolver displays)
    {
        _audit = audit;
        _displays = displays;
    }

    public async Task<Result<PagedList<AuditEntryDto>>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _audit.ListAsync(
            request,
            request.Action,
            request.Entity,
            request.From,
            request.To,
            cancellationToken);

        var rows = page.Items
            .Select(x => (x.Entry, x.AuthorName, Changes: DeserializeChanges(x.Entry.ChangesJson)))
            .ToList();

        // One batched pass: entry labels, FK value labels and user names.
        var resolved = await _displays.ResolvePageAsync(
            rows.Select(x => x.Entry).ToList(),
            CollectValueRefs(rows.SelectMany(x => x.Changes)),
            rows.Select(x => x.Entry.ChangedBy)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList(),
            cancellationToken);

        var items = rows
            .Select(x => x.Entry.ToDto(
                string.IsNullOrWhiteSpace(x.AuthorName) ? null : x.AuthorName,
                AttachValueDisplays(x.Changes, resolved.ValueDisplays),
                resolved.EntryDisplays.GetValueOrDefault(x.Entry.Id)))
            .ToPagedList(page.Page, page.PageSize, page.TotalCount);

        return Result<PagedList<AuditEntryDto>>.Success(items);
    }

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
