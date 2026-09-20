using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Interfaces;

namespace WebApi.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    public const string PermissionClaimType = "permission";
    public const string RoleClaimType = "role";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var subject = _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(subject, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public IReadOnlyList<string> Permissions
        => _httpContextAccessor.HttpContext?.User
            .FindAll(PermissionClaimType)
            .Select(c => c.Value)
            .Distinct()
            .ToList() ?? [];
}