namespace RealEstate.Application.Common.Caching;

/// <summary>
/// Marks a MediatR command as always invalidating a fixed set of entity-type caches once it
/// succeeds. Picked up by CacheInvalidationBehavior. Cascades that only fire conditionally
/// (e.g. a project rename touching Property/Unit caches) are NOT expressed here -- those are
/// bumped inline in the handler, next to the existing conditional snapshot-cascade code, since
/// this interface can only describe what's unconditionally true of the command itself.
/// </summary>
public interface IInvalidatesCache
{
    IReadOnlyCollection<CacheEntityType> AffectedEntityTypes { get; }
}
