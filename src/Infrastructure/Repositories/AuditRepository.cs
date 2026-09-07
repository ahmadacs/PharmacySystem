using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities.Audit;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly ApplicationDbContext _db;

    public AuditRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedList<AuditEntry>> ListAsync(
        PagedQuery paging,
        AuditAction? action,
        string? entity,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var users = _db.Set<ApplicationUser>().AsNoTracking();

        // The author join stays here because Identity tables are invisible from
        // the Application layer; only the author name is used for searching/sorting.
        var data =
            from a in _db.Set<AuditEntry>().AsNoTracking()
            join u in users on a.ChangedBy equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            select new { Entry = a, AuthorName = u != null ? (u.FirstName + " " + u.LastName).Trim() : (string?)null };

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var trimmed = paging.Search.Trim();
            // NOTE: Uses SQL LIKE via Contains ("%search%"). For very large audit tables consider Full-Text Search or trigram indexes
            // to avoid slow sequential scans.
            data = data.Where(x =>
                x.Entry.EntityName.Contains(trimmed) ||
                (x.AuthorName != null && x.AuthorName.Contains(trimmed)));
        }

        if (action.HasValue)
            data = data.Where(x => x.Entry.Action == action.Value);

        if (!string.IsNullOrWhiteSpace(entity))
            data = data.Where(x => x.Entry.EntityName == entity);

        if (from.HasValue)
            data = data.Where(x => x.Entry.ChangedAt >= from.Value);

        if (to.HasValue)
            data = data.Where(x => x.Entry.ChangedAt <= to.Value);

        var totalCount = await data.CountAsync(cancellationToken);

        data = paging.SortBy?.ToLowerInvariant() switch
        {
            "entity" => SortDir(data, x => x.Entry.EntityName, paging.SortDir),
            "action" => SortDir(data, x => x.Entry.Action, paging.SortDir),
            "user" => SortDir(data, x => x.AuthorName, paging.SortDir),
            _ => SortDir(data, x => x.Entry.ChangedAt, paging.SortDir)
        };

        var page = Math.Max(1, paging.Page);
        var pageSize = Math.Clamp(paging.PageSize, 1, 200);

        var items = await data
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Entry)
            .ToListAsync(cancellationToken);

        return items.ToPagedList(page, pageSize, totalCount);
    }

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}