using Domain.Common;
using System.Linq.Expressions;

namespace Application.Common.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : IEntity
{

    IQueryable<TEntity> Query(bool tracked = false);

    Task<List<TResult>> ExecuteListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken = default);

    Task<TResult?> ExecuteFirstOrDefaultAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken = default);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracked = false, CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(Guid id, bool tracked = false, CancellationToken cancellationToken = default);

    Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<List<TResult>> ListAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<List<TResult>> PagedAsync<TResult, TKey>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
    void Remove(TEntity entity);
}
