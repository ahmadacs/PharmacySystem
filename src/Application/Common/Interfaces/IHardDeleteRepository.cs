using Domain.Common;
using System.Linq.Expressions;

namespace Application.Common.Interfaces;

public interface IHardDeleteRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : IHardDelete
{
    Task<int> HardDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}
