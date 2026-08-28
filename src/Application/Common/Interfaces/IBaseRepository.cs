using Domain.Common;

namespace Application.Common.Interfaces;

/// <summary>
/// Adds, updates and removes entities in a persistence store. Querying a single
/// entity by id is supported; complex queues are exposed via the Query() method
/// so the Application layer can build LINQ without depending on any EF-type API.
/// </summary>
public interface IBaseRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns an IQueryable over the entity set (soft-deleted rows are filtered).</summary>
    IQueryable<TEntity> Query();

    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}