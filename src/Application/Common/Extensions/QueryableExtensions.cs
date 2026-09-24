using System.Linq.Expressions;

namespace Application.Common.Extensions;

/// <summary>
/// Single shared ordering helper for all list queries (sortBy/sortDir).
/// Replaces the identical private SortDir method previously duplicated in every list handler.
/// </summary>
public static class QueryableExtensions
{
    public static IOrderedQueryable<T> OrderByDirection<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        string? sortDir)
        => sortDir?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
}
