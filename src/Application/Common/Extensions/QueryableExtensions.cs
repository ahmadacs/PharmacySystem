using System.Linq.Expressions;

namespace Application.Common.Extensions;

public static class QueryableExtensions
{

    public static bool IsDescending(this string? sortDir)
        => sortDir?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true;

    public static IOrderedEnumerable<T> OrderByDirection<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        string? sortDir)
        => sortDir.IsDescending()
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
