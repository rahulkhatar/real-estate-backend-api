using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Interfaces;

namespace RealEstate.Infrastructure.Caching;

/// <summary>
/// In-process replacement for the old Redis-backed cache -- same version-counter invalidation
/// scheme (BumpVersionsAsync orphans keys built against the prior version instead of deleting
/// them), but the counter is now a plain in-memory long since there's no cross-process
/// atomicity to worry about. Cache is per-instance and does not survive a process restart or
/// get shared across replicas; acceptable since this is a performance optimization over
/// MongoDB, not a correctness dependency (see ICacheService).
/// </summary>
public class InMemoryCacheService(IMemoryCache cache, IOptions<CacheSettings> options) : ICacheService
{
    private readonly CacheSettings settings = options.Value;
    private readonly ConcurrentDictionary<CacheEntityType, long> versions = new();

    public Task<T?> GetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, CancellationToken ct = default)
    {
        var key = BuildKey(entityType, requestName, paramsHash);
        var result = cache.TryGetValue(key, out var cached) && cached is string json
            ? JsonSerializer.Deserialize<T>(json)
            : default;

        return Task.FromResult(result);
    }

    public Task SetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, T value, CancellationToken ct = default)
    {
        var key = BuildKey(entityType, requestName, paramsHash);
        cache.Set(key, JsonSerializer.Serialize(value), TimeSpan.FromMinutes(settings.DefaultTtlMinutes));
        return Task.CompletedTask;
    }

    public Task BumpVersionsAsync(IEnumerable<CacheEntityType> entityTypes, CancellationToken ct = default)
    {
        foreach (var entityType in entityTypes.Distinct())
            versions.AddOrUpdate(entityType, 1, (_, current) => current + 1);

        return Task.CompletedTask;
    }

    private string BuildKey(CacheEntityType entityType, string requestName, string paramsHash)
    {
        var version = versions.GetValueOrDefault(entityType);
        return $"{entityType}:v{version}:{requestName}:{paramsHash}";
    }
}
