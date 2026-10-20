using Application.Common.Security;
using Application.Features.AuditLog.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[HasPermission(Permissions.AuditLog.View)]
[ApiVersion("1.0")]
public sealed class AuditLogController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>
    /// Lists audit log entries with pagination, filtering, and search.
    /// </summary>
    /// <param name="query">The paging, filtering, and search parameters.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListAuditEntriesQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);
}