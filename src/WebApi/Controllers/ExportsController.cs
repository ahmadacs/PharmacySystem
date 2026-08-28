using Application.Features.Exports.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
public sealed class ExportsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Exports entity data as Excel or PDF.</summary>
    /// <param name="entityType">medicines|inventory|prescriptions|dispensing</param>
    /// <param name="format">excel|xlsx|pdf</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{entityType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(string entityType, [FromQuery] string format = "excel", CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ExportQuery(entityType, format), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
