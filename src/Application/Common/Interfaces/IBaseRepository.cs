using Application.Common.Models;
using Domain.Common;
using System.Linq.Expressions;

namespace Application.Common.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool tracked = false,
        CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(
        Guid id,
        bool tracked = false,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool tracked = false,
        CancellationToken cancellationToken = default);

    Task<List<TResult>> ListReadAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TResult?> GetReadAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<PagedList<TResult>> PagedAsync<TResult, TKey>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
}
