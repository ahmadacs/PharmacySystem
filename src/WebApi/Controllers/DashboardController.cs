using Application.Features.Dashboard.Queries.GetDashboardSummary;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
[ApiVersion("1.0")]
public sealed class DashboardController(ISender sender) : ApiControllerBase(sender)
{

    /// <summary>
    /// Gets the dashboard summary statistics such as stock levels, low-stock counts, and dispensing totals.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(Application.Features.Dashboard.Dtos.DashboardSummaryDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Summary(CancellationToken cancellationToken)
        => OkResponse(new GetDashboardSummaryQuery(), cancellationToken);
}
