namespace Application.Common.Models;

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

public static class PagedListMapping
{
    public static PagedList<T> ToPagedList<T>(this IEnumerable<T> items, int page, int pageSize, int totalCount)
        => new(items.ToList(), page, pageSize, totalCount);
}
