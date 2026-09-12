using System.Security.Claims;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using WebApi.Services;

namespace WebApi.Caching;

/// <summary>
/// Caches GET/HEAD 200 responses even when the request carries a JWT, while
/// isolating entries by the caller's permission-claim set: two users with
/// different permissions never share the same cached response. The claim type
/// is shared with <see cref="CurrentUserService.PermissionClaimType"/> (which
/// matches the JWT issuance side) so the three sides can never drift apart.
/// Non-200 responses (401/403/4xx) are never stored.
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

        if (allow)
        {
            // Sorted + distinct so claim order can never split or merge entries.
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
