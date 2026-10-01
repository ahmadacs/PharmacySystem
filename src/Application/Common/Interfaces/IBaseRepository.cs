using Application.Common.Models;
using Domain.Common;
using System.Linq.Expressions;

namespace Application.Common.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    /// <summary>
    /// Returns the first entity matching <paramref name="predicate"/>, or null when none matches.
    /// <c>FirstOrDefault</c> is intentional: the predicate is a filter, not a uniqueness assertion.
    /// Prefer a unique database constraint plus fail-fast handling when at most one row is expected.
    /// </summary>
    /// <remarks>
    /// When more than one row matches, the first row is returned with no guaranteed ordering.
    /// Narrow the predicate to a unique key when a deterministic pick matters.
    /// </remarks>
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool tracked = false,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the entity with the given id, or null when it does not exist.</summary>
    Task<TEntity?> GetByIdAsync(
        Guid id,
        bool tracked = false,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all entities matching <paramref name="predicate"/>.</summary>
    /// <remarks>
    /// Ordering is not guaranteed. Use <c>PagedAsync</c> with an explicit
    /// <c>orderBy</c> when a stable order matters.
    /// </remarks>
    Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool tracked = false,
        CancellationToken cancellationToken = default);

    /// <summary>Read-only projection of all rows matching <paramref name="predicate"/>.</summary>
    Task<List<TResult>> ListReadAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Read-only projection of the first row matching <paramref name="predicate"/>.</summary>
    Task<TResult?> GetReadAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of projected rows together with the total matching count.
    /// Page size is clamped to an internal maximum; results are tie-broken by id
    /// so paging stays stable when <paramref name="orderBy"/> values repeat.
    /// </summary>
    Task<PagedList<TResult>> PagedAsync<TResult, TKey>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Counts rows matching <paramref name="predicate"/>.</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Checks whether any row matches <paramref name="predicate"/>.</summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Stages a new entity for insert on the next unit-of-work save.</summary>
    void Add(TEntity entity);

    /// <summary>Stages an entity for delete on the next unit-of-work save.</summary>
    void Remove(TEntity entity);
}
