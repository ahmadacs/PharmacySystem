namespace Domain.Common;

public static class SoftDeleteExtensions
{
    public static IEnumerable<T> NotDeleted<T>(this IEnumerable<T> source)
        where T : ISoftDelete
        => source.Where(item => !item.IsDeleted);
}