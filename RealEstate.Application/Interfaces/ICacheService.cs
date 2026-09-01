using RealEstate.Application.Common.Caching;

namespace RealEstate.Application.Interfaces;

/// <summary>
/// Redis-backed read cache keyed by a per-entity-type version counter rather than explicit key
/// deletion, so invalidation (BumpVersionsAsync) is an O(1) INCR instead of a KEYS/SCAN sweep --
/// stale entries are simply orphaned and age out via TTL.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, CancellationToken ct = default);

    Task SetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, T value, CancellationToken ct = default);

    Task BumpVersionsAsync(IEnumerable<CacheEntityType> entityTypes, CancellationToken ct = default);
}
