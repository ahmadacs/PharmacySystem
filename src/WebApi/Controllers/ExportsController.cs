using Application.Features.Exports.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
public sealed class ExportsController(ISender sender) : ApiControllerBase(sender)
{

    /// <summary>
    /// Exports a report as an Excel or PDF file.
    /// </summary>
    /// <param name="entityType">The report entity type (e.g. inventory, medicines, dispensing, prescriptions).</param>
    /// <param name="format">The output format: excel or pdf.</param>
    /// <param name="id">An optional entity identifier to export a single record.</param>
    [HttpGet("{entityType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Export(string entityType, [FromQuery] string format = "excel", [FromQuery] string? id = null, CancellationToken cancellationToken = default)
        => ExportFileResponse(new ExportQuery(entityType, format, id), cancellationToken);
}
