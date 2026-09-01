using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using Application.Features.AuditLog.Queries;
using Domain.Entities.Audit;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _db;

    public AuditRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedList<AuditEntryDto>> ListAsync(
        ListAuditEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var users = _db.Set<ApplicationUser>().AsNoTracking();

        var data =
            from a in _db.Set<AuditEntry>().AsNoTracking()
            join u in users on a.ChangedBy equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            select new { Entry = a, User = u };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            // NOTE: Uses SQL LIKE via Contains ("%search%"). For very large audit tables consider Full-Text Search or trigram indexes
            // to avoid slow sequential scans.
            data = data.Where(x =>
                x.Entry.EntityName.Contains(search) ||
                (x.User != null && (x.User.FirstName + " " + x.User.LastName).Contains(search)));
        }

        if (query.Action.HasValue)
            data = data.Where(x => x.Entry.Action == query.Action.Value);

        if (!string.IsNullOrWhiteSpace(query.Entity))
            data = data.Where(x => x.Entry.EntityName == query.Entity);

        if (query.From.HasValue)
            data = data.Where(x => x.Entry.ChangedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.Entry.ChangedAt <= query.To.Value);

        var totalCount = await data.CountAsync(cancellationToken);

        data = query.SortBy?.ToLowerInvariant() switch
        {
            "entity" => SortDir(data, x => x.Entry.EntityName, query.SortDir),
            "action" => SortDir(data, x => x.Entry.Action, query.SortDir),
            "user" => SortDir(data, x => x.User!.FirstName + " " + x.User.LastName, query.SortDir),
            _ => SortDir(data, x => x.Entry.ChangedAt, query.SortDir)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var rows = await data
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new AuditEntryDto(
                r.Entry.Id,
                r.Entry.EntityName,
                r.Entry.EntityId,
                r.Entry.Action,
                r.Entry.ChangedBy,
                r.User != null ? (r.User.FirstName + " " + r.User.LastName).Trim() : null,
                r.Entry.ChangedAt,
                DeserializeChanges(r.Entry.ChangesJson)))
            .ToList();

        return new PagedList<AuditEntryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}