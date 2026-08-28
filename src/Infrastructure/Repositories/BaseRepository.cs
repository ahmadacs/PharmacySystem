using Application.Common.Interfaces;
using Domain.Common;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext Db;

    public BaseRepository(ApplicationDbContext db)
    {
        Db = db;
    }

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public IQueryable<TEntity> Query()
        => Db.Set<TEntity>();

    public void Add(TEntity entity)
        => Db.Set<TEntity>().Add(entity);

    public void Update(TEntity entity)
        => Db.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity)
        => Db.Set<TEntity>().Remove(entity);
}