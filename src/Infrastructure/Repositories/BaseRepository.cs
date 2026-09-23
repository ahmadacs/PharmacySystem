using Application.Common.Interfaces;
using Application.Common.Specifications;
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

    public Task<TResult?> GetAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(spec.Selector);

        return Apply(spec, applyPaging: false).Select(spec.Selector).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<TResult>> ListAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(spec.Selector);

        return Apply(spec, applyPaging: true).Select(spec.Selector).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        // Ordering/paging/projection never affect the count; only the filter does,
        // so counting stays a single COUNT query exactly like before.
        IQueryable<TEntity> query = Db.Set<TEntity>();
        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        return query.CountAsync(cancellationToken);
    }

    public void Add(TEntity entity)
        => Db.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity)
        => Db.Set<TEntity>().Remove(entity);

    private IQueryable<TEntity> Apply<TResult>(ISpecification<TEntity, TResult> spec, bool applyPaging)
    {
        IQueryable<TEntity> query = Db.Set<TEntity>();

        if (spec.AsNoTracking)
            query = query.AsNoTracking();

        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        if (spec.OrderBy != null)
            query = spec.OrderBy(query);

        if (applyPaging)
        {
            if (spec.Skip.HasValue)
                query = query.Skip(spec.Skip.Value);
            if (spec.Take.HasValue)
                query = query.Take(spec.Take.Value);
        }

        return query;
    }
}
