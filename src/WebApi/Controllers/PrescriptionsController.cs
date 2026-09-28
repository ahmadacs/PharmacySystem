using Application.Common.Security;
using Application.Features.Prescriptions.Commands;
using Application.Features.Prescriptions.Dtos;
using Application.Features.Prescriptions.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
[ApiVersion("1.0")]
public sealed class PrescriptionsController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet]
    [Authorize(Policy = "Prescriptions.ViewOrOwn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListPrescriptionsQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Prescriptions.ViewOrOwn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => OkResponse(new GetPrescriptionQuery(id), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] CreatePrescriptionRequest request, CancellationToken cancellationToken)
        => Created(nameof(Get), new { id = Guid.Empty }, new CreatePrescriptionCommand(request), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        => NoContent(new CancelPrescriptionCommand(id), cancellationToken);

    [HttpPost("{id:guid}/items/{itemId:guid}/refill")]
    [Authorize(Policy = Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> RefillItem(Guid id, Guid itemId, CancellationToken cancellationToken)
        => NoContent(new RefillPrescriptionCommand(id, [itemId]), cancellationToken);

    [HttpPost("{id:guid}/refill")]
    [Authorize(Policy = Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Refill(Guid id, [FromBody] RefillPrescriptionRequest request, CancellationToken cancellationToken)
        => NoContent(new RefillPrescriptionCommand(id, request.ItemIds), cancellationToken);
}
