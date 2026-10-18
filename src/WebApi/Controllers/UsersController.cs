using Application.Common.Security;
using Application.Features.Users.Commands;
using Application.Features.Users.Dtos;
using Application.Features.Users.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[HasPermission(Permissions.Users.Manage)]
[ApiVersion("1.0")]
public sealed class UsersController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListUsersQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Roles(CancellationToken cancellationToken)
        => OkResponse(new ListRolesQuery(), cancellationToken);

    [HttpGet("roles/{role}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetRolePermissions(string role, CancellationToken cancellationToken)
        => OkResponse(new GetRolePermissionsQuery(role), cancellationToken);

    [HttpPut("roles/{role}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> UpdateRolePermissions(
        string role,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
        => NoContent(new UpdateRolePermissionsCommand(role, request), cancellationToken);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        => Created(nameof(List), new { id = Guid.Empty }, new CreateUserCommand(request), cancellationToken);

    [HttpPatch("{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetActive(Guid id, [FromBody] SetUserActiveRequest request, CancellationToken cancellationToken)
    {
        var setActive = new SetUserActiveCommand(id, request);
        return NoContent(setActive, cancellationToken);
    }
}