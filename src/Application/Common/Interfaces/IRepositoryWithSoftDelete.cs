using Domain.Common;

namespace Application.Common.Interfaces;

public interface IRepositoryWithSoftDelete<TEntity> : IRepository<TEntity>
    where TEntity : ISoftDelete
{
    void SoftDelete(TEntity entity);
}
