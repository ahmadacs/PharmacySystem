using Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WebApi.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; }

    public PermissionRequirement(IReadOnlyList<string> permissions)
    {
        Permissions = permissions;
    }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var granted = context.User.Claims
            .Where(c => c.Type == JwtClaimTypes.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (requirement.Permissions.Any(granted.Contains))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    /// <summary>
    /// Marker for policies that succeed when the caller holds ANY of the listed permissions,
    /// e.g. <c>Permissions.Any:Permissions.Prescriptions.View|Permissions.Prescriptions.ManageOwn</c>.
    /// </summary>
    public const string AnyPrefix = "Permissions.Any:";

    private const string PermissionPrefix = "Permissions.";
    private static readonly char[] Separators = ['|'];

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(AnyPrefix, StringComparison.Ordinal))
        {
            var permissions = policyName[AnyPrefix.Length..]
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return Task.FromResult<AuthorizationPolicy?>(Build(permissions));
        }

        if (policyName.StartsWith(PermissionPrefix, StringComparison.Ordinal))
            return Task.FromResult<AuthorizationPolicy?>(Build([policyName]));

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    private static AuthorizationPolicy Build(IReadOnlyList<string> permissions)
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissions))
            .Build();
}
