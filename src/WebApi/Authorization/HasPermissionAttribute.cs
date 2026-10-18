using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = permission;
    }

    public HasPermissionAttribute(params string[] permissions)
    {
        Policy = PermissionPolicyProvider.AnyPrefix + string.Join('|', permissions);
    }
}
