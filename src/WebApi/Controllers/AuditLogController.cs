using Application.Common.Security;
using Application.Features.AuditLog.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Change-tracking trail: who changed what entity, when, and with which old/new
/// values. Admin-only (Permissions.AuditLog.View).
/// </summary>
[Authorize(Policy = Permissions.AuditLog.View)]
[ApiVersion("1.0")]
public sealed class AuditLogController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListAuditEntriesQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);
}