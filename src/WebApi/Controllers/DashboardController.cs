using Application.Features.Dashboard.Queries.GetDashboardSummary;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Aggregated dashboard statistics in a single query: dispensed today, pending, low-stock and expiring soon.
/// </summary>
[Authorize]
[ApiVersion("1.0")]
public sealed class DashboardController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Returns 4 dashboard counters in one request (single MediatR query, 4 parallel counts).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(Application.Features.Dashboard.Dtos.DashboardSummaryDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Summary(CancellationToken cancellationToken)
        => OkResponse(new GetDashboardSummaryQuery(), cancellationToken);
}
