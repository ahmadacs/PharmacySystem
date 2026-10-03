using Domain.Common;

namespace Application.Common.Interfaces;

public interface IRepositoryWithHardDelete<TEntity> : IRepository<TEntity>
    where TEntity : IHardDelete
{
    void HardDelete(TEntity entity);
}
