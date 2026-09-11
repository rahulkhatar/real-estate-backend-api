using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RealEstate.Application.Common.Behaviors;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Interfaces;
using RealEstate.Infrastructure.Caching;
using Xunit;

namespace RealEstate.Tests.Features;

// Fake DTO: a reference type, mirroring ProjectDto (the real cached response type), since
// value-type TResponse (e.g. int) short-circuits CachingBehavior's "is not null" check
// differently and would not faithfully reproduce the real GetProjectByIdQuery scenario.
public record FakeDto(int CallCount);

// Fake query: cacheable, returns a call counter so we can see whether the handler actually ran.
public record FakeGetQuery(string Id) : IRequest<FakeDto>, ICacheableQuery
{
    public CacheEntityType EntityType => CacheEntityType.Project;
}

public class FakeGetQueryHandler : IRequestHandler<FakeGetQuery, FakeDto>
{
    public static int CallCount;
    public Task<FakeDto> Handle(FakeGetQuery request, CancellationToken ct)
    {
        CallCount++;
        return Task.FromResult(new FakeDto(CallCount));
    }
}

// Fake outer delete command: cascades into a nested Send of FakeInnerDeleteCommand before returning.
public record FakeOuterDeleteCommand : IRequest<Unit>, IInvalidatesCache
{
    public IReadOnlyCollection<CacheEntityType> AffectedEntityTypes => [CacheEntityType.Project];
}

public class FakeOuterDeleteCommandHandler(IMediator mediator) : IRequestHandler<FakeOuterDeleteCommand, Unit>
{
    public async Task<Unit> Handle(FakeOuterDeleteCommand request, CancellationToken ct)
    {
        await mediator.Send(new FakeInnerDeleteCommand(), ct);
        return Unit.Value;
    }
}

public record FakeInnerDeleteCommand : IRequest<Unit>, IInvalidatesCache
{
    public IReadOnlyCollection<CacheEntityType> AffectedEntityTypes => [CacheEntityType.Project];
}

public class FakeInnerDeleteCommandHandler : IRequestHandler<FakeInnerDeleteCommand, Unit>
{
    public Task<Unit> Handle(FakeInnerDeleteCommand request, CancellationToken ct) => Task.FromResult(Unit.Value);
}

public class CachePipelineDiagnosticTests
{
    [Fact]
    public async Task NestedInvalidatingCommand_StillBustsCacheForSubsequentQuery()
    {
        FakeGetQueryHandler.CallCount = 0;

        var services = new ServiceCollection();
        services.AddSingleton<ICacheService, InMemoryCacheService>();
        services.AddSingleton(Options.Create(new CacheSettings { DefaultTtlMinutes = 10 }));
        services.AddMemoryCache();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CachePipelineDiagnosticTests>();
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new FakeGetQuery("x"));
        first.CallCount.Should().Be(1, "first call should be a real cache miss hitting the handler");

        var second = await mediator.Send(new FakeGetQuery("x"));
        second.CallCount.Should().Be(1, "second call should be a cache HIT -- handler must not run again");

        // This is the cascade shape used by DeleteProjectCommandHandler: an outer IInvalidatesCache
        // command whose handler nested-Sends another IInvalidatesCache command before returning.
        await mediator.Send(new FakeOuterDeleteCommand());

        var third = await mediator.Send(new FakeGetQuery("x"));
        third.CallCount.Should().Be(2, "the cascade should have busted the cache -- this must be a fresh handler call, not the stale cached value");
    }

    [Fact]
    public async Task DirectInvalidatingCommand_NoNesting_BustsCache()
    {
        FakeGetQueryHandler.CallCount = 0;

        var services = new ServiceCollection();
        services.AddSingleton<ICacheService, InMemoryCacheService>();
        services.AddSingleton(Options.Create(new CacheSettings { DefaultTtlMinutes = 10 }));
        services.AddMemoryCache();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CachePipelineDiagnosticTests>();
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new FakeGetQuery("x"));
        first.CallCount.Should().Be(1);

        // Directly send the INNER command (no outer wrapper, no nesting) -- isolates whether
        // nesting itself is required to reproduce, or whether invalidation is broken even
        // for a plain top-level command.
        await mediator.Send(new FakeInnerDeleteCommand());

        var second = await mediator.Send(new FakeGetQuery("x"));
        second.CallCount.Should().Be(2, "a direct, non-nested invalidating command should still bust the cache");
    }

    [Fact]
    public async Task BumpVersionsAsync_IsActuallyInvokedByThePipelineForAPlainCommand()
    {
        var services = new ServiceCollection();
        var spy = new SpyCacheService();
        services.AddSingleton<ICacheService>(spy);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CachePipelineDiagnosticTests>();
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new FakeInnerDeleteCommand());

        spy.BumpCallCount.Should().Be(1, "CacheInvalidationBehavior should have called BumpVersionsAsync exactly once for this IInvalidatesCache command");
    }

    private class SpyCacheService : ICacheService
    {
        public int BumpCallCount;
        public Task<T?> GetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, CancellationToken ct = default) =>
            Task.FromResult<T?>(default);
        public Task SetAsync<T>(CacheEntityType entityType, string requestName, string paramsHash, T value, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task BumpVersionsAsync(IEnumerable<CacheEntityType> entityTypes, CancellationToken ct = default)
        {
            BumpCallCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CacheInvalidationBehavior_CalledDirectly_DoesBump()
    {
        var spy = new SpyCacheService();
        var behavior = new CacheInvalidationBehavior<FakeInnerDeleteCommand, MediatR.Unit>(spy);

        var handlerRan = false;
        await behavior.Handle(new FakeInnerDeleteCommand(), () =>
        {
            handlerRan = true;
            return Task.FromResult(MediatR.Unit.Value);
        }, CancellationToken.None);

        handlerRan.Should().BeTrue();
        spy.BumpCallCount.Should().Be(1, "calling the behavior directly, bypassing MediatR's own pipeline wiring, should still bump");
    }
}
