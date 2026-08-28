using System.Reflection;
using Application.Common.Caching;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviours;

/// <summary>
/// Evicts tagged output-cache entries after a successful command. Failures
/// skip eviction so a rejected dispense cannot clear still-valid cache.
/// </summary>
public sealed class CacheInvalidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly InvalidateCacheAttribute? Attribute =
        typeof(TRequest).GetCustomAttribute<InvalidateCacheAttribute>();

    private readonly ICacheInvalidator _cache;
    private readonly ILogger<CacheInvalidationBehaviour<TRequest, TResponse>> _logger;

    public CacheInvalidationBehaviour(
        ICacheInvalidator cache,
        ILogger<CacheInvalidationBehaviour<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next().ConfigureAwait(false);

        if (Attribute is { Tags.Length: > 0 })
        {
            await _cache.EvictByTagsAsync(Attribute.Tags, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(
                "Evicted output cache tags {Tags} after {RequestName}",
                Attribute.Tags,
                typeof(TRequest).Name);
        }

        return response;
    }
}
