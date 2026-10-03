using Domain.Common;

namespace Application.Common.Interfaces;

public interface IBaseRepositoryWithSoftDelete<TEntity> : IBaseRepository<TEntity>
    where TEntity : ISoftDelete
{
    void SoftDelete(TEntity entity);
}
