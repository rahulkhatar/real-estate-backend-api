using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Interfaces;
using StackExchange.Redis;

namespace RealEstate.Infrastructure.Caching;

/// <summary>
/// Every Redis call is wrapped to degrade gracefully -- a down/unreachable Redis must fall back
/// to "no cache" (Get returns nothing, Set/BumpVersions no-op) rather than fail the request, since
/// caching is a performance optimization, not a correctness dependency.
/// </summary>
public class RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisSettings> options, ILogger<RedisCacheService> logger)
    : ICacheService
{
    private readonly RedisSettings settings = options.Value;

    public async Task<T?> GetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var key = await BuildKeyAsync(db, entityType, requestName, paramsHash);
            var value = await db.StringGetAsync(key);

            return value.HasValue ? JsonSerializer.Deserialize<T>((string)value!) : default;
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis GET failed for {EntityType}:{RequestName}; treating as a cache miss.", entityType, requestName);
            return default;
        }
    }

    public async Task SetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, T value, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var key = await BuildKeyAsync(db, entityType, requestName, paramsHash);
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, TimeSpan.FromMinutes(settings.DefaultTtlMinutes));
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis SET failed for {EntityType}:{RequestName}; response was not cached.", entityType, requestName);
        }
    }

    public async Task BumpVersionsAsync(IEnumerable<CacheEntityType> entityTypes, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            foreach (var entityType in entityTypes.Distinct())
                await db.StringIncrementAsync(VersionKey(entityType));
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis version bump failed; cached entries may serve stale data until their TTL expires.");
        }
    }

    // Reads whatever key/paramsHash combination is current for this entity type's version, so a
    // BumpVersionsAsync call instantly orphans every key built against the prior version -- no
    // key deletion or KEYS/SCAN needed, orphaned entries just expire via their own TTL.
    private async Task<string> BuildKeyAsync(IDatabase db, CacheEntityType entityType, string requestName, string paramsHash)
    {
        var version = await db.StringGetAsync(VersionKey(entityType));
        // Absent counter reads as 0 here (not 1) so the very first BumpVersionsAsync call --
        // which INCRs a missing key from an implicit 0 to 1 -- actually changes the built key.
        var versionNumber = version.HasValue ? (long)version : 0;
        return $"{settings.InstanceName}{entityType}:v{versionNumber}:{requestName}:{paramsHash}";
    }

    private string VersionKey(CacheEntityType entityType) => $"{settings.InstanceName}version:{entityType}";
}
