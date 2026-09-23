using System.Linq.Expressions;

namespace Application.Common.Specifications;

/// <summary>
/// Describes a single-entity query: filter + ordering + paging + projection.
/// The object carries only data (expressions); execution lives in Infrastructure,
/// so the repository stays fully generic with no business logic and no Includes.
/// Cross-entity single-round-trip reads (dashboard snapshot, batch page with
/// dispensing totals) cannot be expressed by one single-entity spec without
/// extra round trips, so those two cases use dedicated single-query read
/// methods on the repositories instead of leaking IQueryable.
/// </summary>
public interface ISpecification<T, TResult>
{
    /// <summary>Combined WHERE predicate (null = no filter).</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Projection applied after filtering/ordering/paging. Required.</summary>
    Expression<Func<T, TResult>> Selector { get; }

    /// <summary>Ordering applied on the entity before paging. Null = unordered.</summary>
    Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; }

    int? Skip { get; }
    int? Take { get; }

    /// <summary>True (default) = AsNoTracking read. False = tracked (for updates).</summary>
    bool AsNoTracking { get; }
}
