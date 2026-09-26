using System.Linq.Expressions;

namespace Application.Common.Extensions;

/// <summary>
/// Single shared ordering helper for all list queries (sortBy/sortDir).
/// </summary>
public static class QueryableExtensions
{
    /// <summary>"desc" (any case) = descending.</summary>
    public static bool IsDescending(this string? sortDir)
        => sortDir?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// In-memory ordering for small projected lists (sort key must be part of
    /// the projected row).
    /// </summary>
    public static IOrderedEnumerable<T> OrderByDirection<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        string? sortDir)
        => sortDir.IsDescending()
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
