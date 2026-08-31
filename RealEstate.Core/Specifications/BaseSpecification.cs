using System.Linq.Expressions;

namespace RealEstate.Core.Specifications;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification() { }

    protected BaseSpecification(Expression<Func<T, bool>> criteria) => AddCriteria(criteria);

    public List<Expression<Func<T, bool>>> Criteria { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Skip { get; private set; }
    public int Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria) => Criteria.Add(criteria);

    protected void AddCriteriaIf(bool condition, Expression<Func<T, bool>> criteria)
    {
        if (condition) Criteria.Add(criteria);
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) => OrderBy = orderByExpression;

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression) =>
        OrderByDescending = orderByDescExpression;

    protected void ApplyPaging(int pageNumber, int pageSize)
    {
        Skip = Math.Max(pageNumber - 1, 0) * pageSize;
        Take = pageSize;
        IsPagingEnabled = true;
    }
}
