using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PagedList() { }

    public PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedList<T> Empty(int page = 1, int pageSize = 10) => new([], page, pageSize, 0);
}
