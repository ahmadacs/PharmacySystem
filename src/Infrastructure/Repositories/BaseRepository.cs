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

    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracked = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        IQueryable<TEntity> query = Db.Set<TEntity>();
        if (!tracked)
            query = query.AsNoTracking();

        return query.Where(predicate).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TEntity?> GetByIdAsync(Guid id, bool tracked = false, CancellationToken cancellationToken = default)
        => GetAsync(e => e.Id == id, tracked, cancellationToken);

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = Db.Set<TEntity>();
        if (predicate is not null)
            query = query.Where(predicate);

        return query.ToListAsync(cancellationToken);
    }

    public Task<List<TResult>> ListAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);

        return Db.Set<TEntity>().AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);

        return Db.Set<TEntity>().AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
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

        var filtered = Db.Set<TEntity>().AsNoTracking().Where(predicate);
        var ordered = descending ? filtered.OrderByDescending(orderBy) : filtered.OrderBy(orderBy);

        return ordered
            .Select(selector)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = Db.Set<TEntity>();
        if (predicate is not null)
            query = query.Where(predicate);

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Db.Set<TEntity>().AnyAsync(predicate, cancellationToken);
    }

    public void Add(TEntity entity)
        => Db.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity)
        => Db.Set<TEntity>().Remove(entity);
}
