using System.Linq.Expressions;
using Application.Common.Models;
using Domain.Common;

namespace Application.Common.Interfaces;

/// <summary>
/// Generic repository with expression-based reads: callers pass filter /
/// projection as expressions — no business logic, no Includes, no IQueryable
/// leaking to callers. Updates need no method: entities are EF-tracked, so
/// mutated graphs persist on SaveChanges; Add/Remove only change membership.
/// </summary>
public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    /// <summary>First entity matching the predicate (null when none). Tracked only when it will be mutated.</summary>
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracked = false, CancellationToken cancellationToken = default);

    /// <summary>Single entity by id. Tracked only when it will be mutated.</summary>
    Task<TEntity?> GetByIdAsync(Guid id, bool tracked = false, CancellationToken cancellationToken = default);

    /// <summary>Entities matching the predicate (tracked).</summary>
    Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Projected rows: only the selector's columns are fetched.</summary>
    Task<List<TResult>> ListAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Single projected row: only the selector's columns are fetched.</summary>
    Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projected page: ORDER BY + OFFSET/FETCH in SQL with the projection
    /// applied after ordering. Pair with <see cref="CountAsync"/> for the
    /// total (two queries instead of fetching everything). The order key is
    /// an <b>entity</b> expression: ordering over a constructed DTO/record
    /// is untranslatable — EF cannot resolve <c>new Row(...).Member</c>
    /// in ORDER BY (anonymous types excepted).
    /// </summary>
    Task<List<TResult>> PagedAsync<TResult, TKey>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Count of entities matching the predicate (null = all).</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>True when any entity matches the predicate.</summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
    void Remove(TEntity entity);
}
