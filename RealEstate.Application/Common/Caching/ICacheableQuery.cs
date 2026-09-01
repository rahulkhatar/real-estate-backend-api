namespace RealEstate.Application.Common.Caching;

/// <summary>
/// Marks a MediatR query as safe to serve from Redis. Implemented by list/detail queries for
/// Projects, Properties, Units, and UnitLayouts; picked up by CachingBehavior via a type check
/// so no query handler needs caching code of its own.
/// </summary>
public interface ICacheableQuery
{
    CacheEntityType EntityType { get; }
}
