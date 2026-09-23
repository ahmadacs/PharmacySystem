using Application.Common.Specifications;
using Domain.Common;

namespace Application.Common.Interfaces;

/// <summary>
/// Fully generic repository: every read goes through an
/// <see cref="ISpecification{TEntity, TResult}"/> (filter + ordering + paging +
/// projection) — including single-entity lookup by id, expressed as a spec.
/// The repository applies only what the spec carries — no business logic, no
/// Includes, no IQueryable leaking to callers.
/// The <see cref="Domain.Common.IEntity"/> constraint keeps the repository
/// for domain entities with an identity — never arbitrary types — without
/// imposing <see cref="Domain.Common.BaseEntity"/> behavior (soft-delete,
/// audit fields, domain events) on standalone rows like audit entries.
/// Note: updates need no method — entities are EF-tracked, so mutated graphs
/// persist on SaveChanges; Add/Remove only change set membership.
/// </summary>
public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    /// <summary>First projected row matching the spec (filter + ordering applied).</summary>
    Task<TResult?> GetAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken = default);

    /// <summary>All projected rows matching the spec (filter + ordering + paging applied).</summary>
    Task<List<TResult>> ListAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken = default);

    /// <summary>Count of entities matching the spec filter (ordering/paging/selector ignored).</summary>
    Task<int> CountAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
    void Remove(TEntity entity);
}