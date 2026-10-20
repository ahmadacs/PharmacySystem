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

    /// <summary>
    /// Lists users with pagination, filtering, sorting, and search.
    /// </summary>
    /// <param name="query">The paging, filtering, sorting, and search parameters.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListUsersQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>
    /// Lists all available roles.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Roles(CancellationToken cancellationToken)
        => OkResponse(new ListRolesQuery(), cancellationToken);

    /// <summary>
    /// Gets the permissions assigned to a role.
    /// </summary>
    /// <param name="role">The role name.</param>
    [HttpGet("roles/{role}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetRolePermissions(string role, CancellationToken cancellationToken)
        => OkResponse(new GetRolePermissionsQuery(role), cancellationToken);

    /// <summary>
    /// Replaces the permissions assigned to a role.
    /// </summary>
    /// <param name="role">The role name.</param>
    /// <param name="request">The new set of permissions.</param>
    [HttpPut("roles/{role}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> UpdateRolePermissions(
        string role,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
        => NoContent(new UpdateRolePermissionsCommand(role, request), cancellationToken);

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">The new user details and role.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        => Created(nameof(List), new { id = Guid.Empty }, new CreateUserCommand(request), cancellationToken);

    /// <summary>
    /// Activates or deactivates a user account.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="request">The desired active state.</param>
    [HttpPatch("{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetActive(Guid id, [FromBody] SetUserActiveRequest request, CancellationToken cancellationToken)
    {
        var setActive = new SetUserActiveCommand(id, request);
        return NoContent(setActive, cancellationToken);
    }
}