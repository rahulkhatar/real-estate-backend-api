using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Application.Common.Caching;
using RealEstate.Application.Interfaces;

namespace RealEstate.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse>(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        var requestName = typeof(TRequest).Name;
        var paramsHash = HashRequest(request);

        var cached = await cache.GetAsync<TResponse>(cacheable.EntityType, requestName, paramsHash, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Cache HIT {EntityType}:{RequestName}:{ParamsHash}", cacheable.EntityType, requestName, paramsHash);
            return cached;
        }

        logger.LogInformation("Cache MISS {EntityType}:{RequestName}:{ParamsHash}", cacheable.EntityType, requestName, paramsHash);
        var response = await next();
        await cache.SetAsync(cacheable.EntityType, requestName, paramsHash, response, cancellationToken);
        return response;
    }

    private static string HashRequest(TRequest request)
    {
        var json = JsonSerializer.Serialize(request, request!.GetType());
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash)[..16];
    }
}
