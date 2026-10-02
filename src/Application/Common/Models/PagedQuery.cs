using System.ComponentModel.DataAnnotations;
using Application.Common.Extensions;

namespace Application.Common.Models;

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

    public virtual int MaxPageSize => 100;

    public int EffectivePage => Math.Max(1, Page);

    public int EffectivePageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    public PaginationParams ToPagination()
        => new(EffectivePage, EffectivePageSize, SortDir.IsDescending());
}
