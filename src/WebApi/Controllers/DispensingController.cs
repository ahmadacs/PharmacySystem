using Application.Common.Security;
using Application.Features.Dispensing.Commands;
using Application.Features.Dispensing.Dtos;
using Application.Features.Dispensing.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[Authorize]
[ApiVersion("1.0")]
public sealed class DispensingController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet]
    [HasPermission(Permissions.Dispensing.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] DispensingRecordListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpGet("lookup")]
    [HasPermission(Permissions.Dispensing.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Lookup(
        [FromQuery] LookupPrescriptionForDispensingQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpPost]
    [HasPermission(Permissions.Dispensing.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Dispense([FromBody] DispensePrescriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DispensePrescriptionCommand(request), cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : FailureResponse(result);
    }
}