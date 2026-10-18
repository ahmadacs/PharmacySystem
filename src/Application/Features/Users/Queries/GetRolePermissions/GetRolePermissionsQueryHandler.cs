using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, Result<RolePermissionsDto>>
{
    private readonly IPermissionService _permissions;

    public GetRolePermissionsQueryHandler(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    public async Task<Result<RolePermissionsDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var role = request.Role.Trim();
        var assigned = await _permissions.GetRolePermissionsAsync(role, cancellationToken);
        if (assigned is null)
            return Result<RolePermissionsDto>.Failure($"Role '{role}' was not found.", 404);

        var catalog = await _permissions.GetCatalogAsync(cancellationToken);

        return Result<RolePermissionsDto>.Success(
            new RolePermissionsDto(role, assigned, catalog));
    }
}
