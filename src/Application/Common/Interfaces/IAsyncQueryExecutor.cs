namespace Application.Common.Interfaces;

/// <summary>
/// Executes async LINQ over IQueryables. The Application layer can build queries
/// using only BCL types; the implementation (EF Core in Infrastructure) provides
/// the actual async execution so requests don't block the worker thread.
/// </summary>
public interface IAsyncQueryExecutor
{
    Task<int> CountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default);
    Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default);
    Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default);
}