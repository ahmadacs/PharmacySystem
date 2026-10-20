using Application.Common.Security;
using Application.Features.Prescriptions.Commands;
using Application.Features.Prescriptions.Dtos;
using Application.Features.Prescriptions.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[Authorize]
[ApiVersion("1.0")]
public sealed class PrescriptionsController(ISender sender) : ApiControllerBase(sender)
{

    /// <summary>
    /// Lists prescriptions with pagination, filtering, sorting, and search. Doctors only see their own prescriptions.
    /// </summary>
    /// <param name="query">The paging, filtering, sorting, and search parameters.</param>
    [HttpGet]
    [HasPermission(Permissions.Prescriptions.View, Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListPrescriptionsQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>
    /// Gets a prescription by its identifier. Doctors can only access their own prescriptions.
    /// </summary>
    /// <param name="id">The prescription identifier.</param>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Prescriptions.View, Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => OkResponse(new GetPrescriptionQuery(id), cancellationToken);

    /// <summary>
    /// Creates a new prescription for a patient.
    /// </summary>
    /// <param name="request">The prescription and its items.</param>
    [HttpPost]
    [HasPermission(Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] CreatePrescriptionRequest request, CancellationToken cancellationToken)
        => Created(nameof(Get), new { id = Guid.Empty }, new CreatePrescriptionCommand(request), cancellationToken);

    /// <summary>
    /// Cancels a prescription. Doctors can only cancel their own prescriptions.
    /// </summary>
    /// <param name="id">The prescription identifier.</param>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        => NoContent(new CancelPrescriptionCommand(id), cancellationToken);

    /// <summary>
    /// Refills a single prescription item after validating refill eligibility.
    /// </summary>
    /// <param name="id">The prescription identifier.</param>
    /// <param name="itemId">The prescription item identifier.</param>
    [HttpPost("{id:guid}/items/{itemId:guid}/refill")]
    [HasPermission(Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> RefillItem(Guid id, Guid itemId, CancellationToken cancellationToken)
        => NoContent(new RefillPrescriptionCommand(id, [itemId]), cancellationToken);

    /// <summary>
    /// Refills the specified prescription items after validating refill eligibility.
    /// </summary>
    /// <param name="id">The prescription identifier.</param>
    /// <param name="request">The identifiers of the items to refill.</param>
    [HttpPost("{id:guid}/refill")]
    [HasPermission(Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Refill(Guid id, [FromBody] RefillPrescriptionRequest request, CancellationToken cancellationToken)
        => NoContent(new RefillPrescriptionCommand(id, request.ItemIds), cancellationToken);
}
