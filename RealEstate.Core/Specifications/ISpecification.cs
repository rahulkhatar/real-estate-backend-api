using System.Linq.Expressions;

namespace RealEstate.Core.Specifications;

/// <summary>
/// MongoDB-flavored specification: filter + sort + paging only.
/// There is no EF-style "Include" — MongoDB has no navigation-property joins,
/// so any joined data is fetched via an explicit $lookup aggregation instead.
/// </summary>
public interface ISpecification<T>
{
    List<Expression<Func<T, bool>>> Criteria { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int Skip { get; }
    int Take { get; }
    bool IsPagingEnabled { get; }
}
