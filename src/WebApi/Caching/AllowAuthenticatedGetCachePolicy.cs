using System.Security.Claims;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using WebApi.Services;

namespace WebApi.Caching;

public sealed class AllowAuthenticatedGetCachePolicy : IOutputCachePolicy
{
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var method = context.HttpContext.Request.Method;
        var allow = HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        context.EnableOutputCaching = allow;
        context.AllowCacheLookup = allow;
        context.AllowCacheStorage = allow;
        context.AllowLocking = true;

        if (allow)
        {

            var permissions = context.HttpContext.User
                .FindAll(CurrentUserService.PermissionClaimType)
                .Select(c => c.Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(v => v, StringComparer.Ordinal);
            context.CacheVaryByRules.VaryByValues["permission"] = string.Join(",", permissions);
        }

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
