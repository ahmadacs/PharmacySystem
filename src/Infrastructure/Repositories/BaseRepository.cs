using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Common;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly ApplicationDbContext Db;

    public BaseRepository(ApplicationDbContext db)
    {
        Db = db;
    }

    public IQueryable<TEntity> Query(bool tracked = false)
    {
        IQueryable<TEntity> query = Db.Set<TEntity>();
        return tracked ? query : query.AsNoTracking();
    }

    public Task<List<TResult>> ExecuteListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return query.ToListAsync(cancellationToken);
    }

    public Task<TResult?> ExecuteFirstOrDefaultAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracked = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return ExecuteFirstOrDefaultAsync(Query(tracked).Where(predicate), cancellationToken);
    }

    public Task<TEntity?> GetByIdAsync(Guid id, bool tracked = false, CancellationToken cancellationToken = default)
        => GetAsync(e => e.Id == id, tracked, cancellationToken);

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query(tracked: true);
        if (predicate is not null)
            query = query.Where(predicate);

        return ExecuteListAsync(query, cancellationToken);
    }

    public Task<List<TResult>> ListAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);

        return ExecuteListAsync(Query().Where(predicate).Select(selector), cancellationToken);
    }

    public Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);

        return ExecuteFirstOrDefaultAsync(Query().Where(predicate).Select(selector), cancellationToken);
    }

    public Task<List<TResult>> PagedAsync<TResult, TKey>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(orderBy);

        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var filtered = Query().Where(predicate);
        var ordered = descending ? filtered.OrderByDescending(orderBy) : filtered.OrderBy(orderBy);

        return ExecuteListAsync(
            ordered
                .Select(selector)
                .Skip((page - 1) * pageSize)
                .Take(pageSize),
            cancellationToken);
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = Query(tracked: true);
        if (predicate is not null)
            query = query.Where(predicate);

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Query(tracked: true).AnyAsync(predicate, cancellationToken);
    }

    public void Add(TEntity entity)
        => Db.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity)
        => Db.Set<TEntity>().Remove(entity);
}
