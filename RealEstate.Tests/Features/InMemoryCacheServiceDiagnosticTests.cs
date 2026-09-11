using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RealEstate.Application.Common.Caching;
using RealEstate.Infrastructure.Caching;
using Xunit;

namespace RealEstate.Tests.Features;

public class InMemoryCacheServiceDiagnosticTests
{
    private static InMemoryCacheService CreateService() =>
        new(new MemoryCache(new MemoryCacheOptions()), Options.Create(new CacheSettings { DefaultTtlMinutes = 10 }));

    [Fact]
    public async Task BumpVersionsAsync_InvalidatesPriorCachedValueForSameParams()
    {
        var cache = CreateService();

        await cache.SetAsync(CacheEntityType.Project, "GetProjectByIdQuery", "hash1", "original-value", CancellationToken.None);
        var beforeBump = await cache.GetAsync<string>(CacheEntityType.Project, "GetProjectByIdQuery", "hash1", CancellationToken.None);
        beforeBump.Should().Be("original-value");

        await cache.BumpVersionsAsync([CacheEntityType.Project], CancellationToken.None);

        var afterBump = await cache.GetAsync<string>(CacheEntityType.Project, "GetProjectByIdQuery", "hash1", CancellationToken.None);
        afterBump.Should().BeNull("the version bump should orphan the key set before it, forcing a fresh fetch");
    }
}
