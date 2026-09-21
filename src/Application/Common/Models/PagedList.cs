namespace Application.Common.Models;

/// <summary>
/// Generic PagedList — holds a page of items with pagination metadata.
/// Single canonical paginated type for the solution.
/// </summary>
public sealed class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }

    public PagedList() { }

    public PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}

/// <summary>
/// Single canonical way to wrap a materialized page: handlers map items via
/// ToDto() first, then call this. No inline <c>new PagedList&lt;T&gt;</c> in handlers.
/// </summary>
public static class PagedListMapping
{
    public static PagedList<T> ToPagedList<T>(this IEnumerable<T> items, int page, int pageSize, int totalCount)
        => new(items.ToList(), page, pageSize, totalCount);
}
