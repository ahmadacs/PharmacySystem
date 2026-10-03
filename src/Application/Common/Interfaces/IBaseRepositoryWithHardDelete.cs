using Domain.Common;

namespace Application.Common.Interfaces;

public interface IBaseRepositoryWithHardDelete<TEntity> : IBaseRepository<TEntity>
    where TEntity : IHardDelete
{
    void HardDelete(TEntity entity);
}
