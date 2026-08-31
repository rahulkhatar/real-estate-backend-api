using MongoDB.Driver;
using RealEstate.Core.Specifications;

namespace RealEstate.Infrastructure.Persistence.Specifications;

/// <summary>
/// Turns an ISpecification into a MongoDB LINQ query. There is no "Include" step here —
/// MongoDB has no navigation-property joins, so joined data is fetched via an explicit
/// $lookup aggregation elsewhere when actually needed, not through the specification.
/// </summary>
public static class SpecificationEvaluator<T>
{
    public static IQueryable<T> GetQuery(IMongoCollection<T> collection, ISpecification<T> spec)
    {
        var query = collection.AsQueryable();

        query = spec.Criteria.Aggregate(query, (current, criteria) => current.Where(criteria));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);

        return query;
    }

    public static IQueryable<T> GetCountQuery(IMongoCollection<T> collection, ISpecification<T> spec)
    {
        var query = collection.AsQueryable();
        return spec.Criteria.Aggregate(query, (current, criteria) => current.Where(criteria));
    }
}
