using Application.Common.Interfaces;
using Domain.Common;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class RepositoryWithHardDelete<TEntity> : Repository<TEntity>, IRepositoryWithHardDelete<TEntity>
    where TEntity : class, IHardDelete
{
    public RepositoryWithHardDelete(ApplicationDbContext db) : base(db)
    {
    }

    public void HardDelete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Db.Set<TEntity>().Remove(entity);
    }
}
