using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Features.Users.Commands;

public sealed class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    private readonly IPermissionService _permissions;

    public UpdateRolePermissionsCommandHandler(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = request.Role.Trim();
        if (string.IsNullOrWhiteSpace(role))
            return Result.Failure("Role is required.", 400);

        var (found, invalid) = await _permissions.SetRolePermissionsAsync(
            role, request.Request.Permissions, cancellationToken);

        if (!found)
            return Result.Failure($"Role '{role}' was not found.", 404);

        if (invalid.Count > 0)
            return Result.Failure($"Unknown permission(s): {string.Join(", ", invalid)}.", 422);

        return Result.Success();
    }
}
