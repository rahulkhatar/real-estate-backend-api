using MediatR;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Interfaces;

namespace RealEstate.Application.Common.Behaviors;

public class CacheInvalidationBehavior<TRequest, TResponse>(ICacheService cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Only bump on success -- a failed command shouldn't touch the version counters.
        if (request is IInvalidatesCache invalidates)
            await cache.BumpVersionsAsync(invalidates.AffectedEntityTypes, cancellationToken);

        return response;
    }
}
