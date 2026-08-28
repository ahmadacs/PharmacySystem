using Application.Common.Security;
using Application.Features.Users.Commands;
using Application.Features.Users.Dtos;
using Application.Features.Users.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// User administration: list users and roles, create users and toggle account
/// activation. Admin-only (Permissions.Users.Manage).
/// </summary>
[Authorize(Policy = Permissions.Users.Manage)]
[ApiVersion("1.0")]
public sealed class UsersController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Lists users with pagination, search and role/status filtering.</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir, role, active.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListUsersQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Lists the assignable roles (Admin, Pharmacist, Doctor).</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Roles(CancellationToken cancellationToken)
        => OkResponse(new ListRolesQuery(), cancellationToken);

    /// <summary>Creates a user account with a role and initial password.</summary>
    /// <param name="request">Names, email, role and password.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        => Created(nameof(List), new { id = Guid.Empty }, new CreateUserCommand(request), cancellationToken);

    /// <summary>Activates or deactivates a user account.</summary>
    /// <param name="id">User id.</param>
    /// <param name="request">Active flag.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPatch("{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetActive(Guid id, [FromBody] SetUserActiveRequest request, CancellationToken cancellationToken)
    {
        var setActive = new SetUserActiveCommand(id, request);
        return NoContent(setActive, cancellationToken);
    }
}