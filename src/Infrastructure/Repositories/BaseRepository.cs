using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Common;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class, IEntity
{
    private const int MaxPageSize = 200;

    protected readonly ApplicationDbContext Db;

    public BaseRepository(ApplicationDbContext db)
    {
        Db = db;
    }

    protected IQueryable<TEntity> Query(bool tracked = false)
    {
        IQueryable<TEntity> query = Db.Set<TEntity>();
        return tracked ? query : query.AsNoTracking();
    }

    public Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool tracked = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Query(tracked)
            .Where(predicate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TEntity?> GetByIdAsync(
        Guid id,
        bool tracked = false,
        CancellationToken cancellationToken = default)
        => GetAsync(e => e.Id == id, tracked, cancellationToken);

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool tracked = false,
        CancellationToken cancellationToken = default)
    {
        var query = Query(tracked);
        if (predicate is not null)
            query = query.Where(predicate);

        return query.ToListAsync(cancellationToken);
    }

    public Task<List<TResult>> ListReadAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);

        return Query()
            .Where(predicate)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public Task<TResult?> GetReadAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);

        return Query()
            .Where(predicate)
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedList<TResult>> PagedAsync<TResult, TKey>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(orderBy);

        var page = Math.Max(1, pagination.Page);
        var pageSize = Math.Clamp(pagination.PageSize, 1, MaxPageSize);
        var descending = pagination.Descending;

        var filtered = Query().Where(predicate);
        var totalCount = await filtered.CountAsync(cancellationToken);
        if (totalCount == 0)
            return new PagedList<TResult>([], page, pageSize, 0);

        IQueryable<TEntity> ordered = descending
            ? filtered.OrderByDescending(orderBy).ThenByDescending(e => e.Id)
            : filtered.OrderBy(orderBy).ThenBy(e => e.Id);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedList<TResult>(items, page, pageSize, totalCount);
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = Query();
        if (predicate is not null)
            query = query.Where(predicate);

        return query.CountAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Query().AnyAsync(predicate, cancellationToken);
    }

    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Db.Set<TEntity>().Add(entity);
    }
}
