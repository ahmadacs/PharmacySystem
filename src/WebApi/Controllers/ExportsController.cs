using Application.Features.Exports.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
public sealed class ExportsController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet("{entityType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Export(string entityType, [FromQuery] string format = "excel", [FromQuery] string? id = null, CancellationToken cancellationToken = default)
        => ExportFileResponse(new ExportQuery(entityType, format, id), cancellationToken);
}
