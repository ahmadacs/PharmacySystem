using Application.Common.Interfaces;
using Domain.Common;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class BaseRepositoryWithSoftDelete<TEntity> : BaseRepository<TEntity>, IBaseRepositoryWithSoftDelete<TEntity>
    where TEntity : class, ISoftDelete
{
    public BaseRepositoryWithSoftDelete(ApplicationDbContext db) : base(db)
    {
    }

    public void SoftDelete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        // Interceptor يحول Deleted -> Modified + IsDeleted = true
        Db.Set<TEntity>().Remove(entity);
    }
}
