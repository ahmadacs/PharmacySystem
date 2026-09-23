using System.Linq.Expressions;

namespace Application.Common.Specifications;

/// <summary>
/// Mutable builder for <see cref="ISpecification{T, TResult}"/>. Handlers compose
/// filter + ordering + paging + projection here (business rules live in the
/// caller); the repository only applies what the spec carries.
/// </summary>
public sealed class Specification<T, TResult> : ISpecification<T, TResult>
{
    public Specification(Expression<Func<T, TResult>> selector)
    {
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }

    public Expression<Func<T, TResult>> Selector { get; }

    public Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; private set; }

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool AsNoTracking { get; private set; } = true;

    /// <summary>ANDs an additional predicate into <see cref="Criteria"/>.</summary>
    public Specification<T, TResult> Where(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (Criteria is null)
        {
            Criteria = predicate;
            return this;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var left = new ParameterReplacer(Criteria.Parameters[0], parameter).Visit(Criteria.Body)!;
        var right = new ParameterReplacer(predicate.Parameters[0], parameter).Visit(predicate.Body)!;
        Criteria = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        return this;
    }

    public Specification<T, TResult> Order(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
    {
        OrderBy = orderBy ?? throw new ArgumentNullException(nameof(orderBy));
        return this;
    }

    public Specification<T, TResult> Page(int skip, int take)
    {
        Skip = skip;
        Take = take;
        return this;
    }

    /// <summary>Keep EF change tracking on (needed when the loaded entities will be mutated).</summary>
    public Specification<T, TResult> Tracked()
    {
        AsNoTracking = false;
        return this;
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
