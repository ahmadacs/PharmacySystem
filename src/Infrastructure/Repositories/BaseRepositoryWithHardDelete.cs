using Application.Common.Interfaces;
using Domain.Common;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class BaseRepositoryWithHardDelete<TEntity> : BaseRepository<TEntity>, IBaseRepositoryWithHardDelete<TEntity>
    where TEntity : class, IHardDelete
{
    public BaseRepositoryWithHardDelete(ApplicationDbContext db) : base(db)
    {
    }

    public void HardDelete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Db.Set<TEntity>().Remove(entity);
    }
}
