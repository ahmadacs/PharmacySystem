using Application.Common.Interfaces;
using Domain.Common;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public sealed class HardDeleteRepository<TEntity> : BaseRepository<TEntity>, IHardDeleteRepository<TEntity>
    where TEntity : class, IHardDelete
{
    public HardDeleteRepository(ApplicationDbContext db) : base(db)
    {
    }

    public Task<int> HardDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Db.Set<TEntity>().Where(predicate).ExecuteDeleteAsync(cancellationToken);
    }
}
