using System.ComponentModel.DataAnnotations;

namespace Application.Common.Models;

/// <summary>
/// Shared paging/sorting/filtering envelope for list queries. Feature queries
/// inherit from it and add their own filters; validation rules and paging
/// normalization are defined once here instead of being repeated per query.
/// </summary>
public abstract record PagedQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    public virtual int Page { get; init; } = 1;

    [Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    public virtual int PageSize { get; init; } = 10;

    [StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    public virtual string? Search { get; init; }

    [StringLength(50, ErrorMessage = "SortBy must be at most 50 characters.")]
    public virtual string? SortBy { get; init; }

    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
    public virtual string SortDir { get; init; } = "desc";
    /// <summary>1-based page number, at least 1.</summary>
    public int NormalizedPage => Math.Max(1, Page);

    /// <summary>Page size clamped to 1..<paramref name="maxPageSize"/>.</summary>
    public int NormalizedPageSize(int maxPageSize = 100) => Math.Clamp(PageSize, 1, maxPageSize);
}
