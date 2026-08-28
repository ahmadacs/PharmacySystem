namespace Domain.Common;

/// <summary>
/// Extension helpers that remove the repeated `!IsDeleted` guard from queries
/// over soft-deletable entities. The soft-delete rule lives in exactly one
/// place: the "not deleted" predicate below.
///
/// Note: these are for in-memory collections (the private lists behind
/// aggregate navigation properties, DTO mapping, ...). Database queries need
/// no such guard at all — the EF Core global query filter (see
/// ApplicationDbContext.ApplySoftDeleteFilters) already excludes deleted rows.
/// </summary>
public static class SoftDeleteExtensions
{
    public static IEnumerable<T> NotDeleted<T>(this IEnumerable<T> source)
        where T : ISoftDelete
        => source.Where(item => !item.IsDeleted);
}