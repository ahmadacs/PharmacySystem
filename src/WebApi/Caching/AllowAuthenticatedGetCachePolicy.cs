using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace WebApi.Caching;

/// <summary>
/// Caches GET/HEAD 200 responses even when the request carries a JWT.
/// Does not vary by Authorization, so users who share the same view permission
/// reuse one entry. Non-200 responses (401/403/4xx) are never stored.
/// </summary>
public sealed class AllowAuthenticatedGetCachePolicy : IOutputCachePolicy
{
    public static readonly AllowAuthenticatedGetCachePolicy Instance = new();

    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var method = context.HttpContext.Request.Method;
        var allow = HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        context.EnableOutputCaching = allow;
        context.AllowCacheLookup = allow;
        context.AllowCacheStorage = allow;
        context.AllowLocking = true;

        return ValueTask.CompletedTask;
    }

    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;

        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie)
            || response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }
}
