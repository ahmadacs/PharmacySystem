using Application.Common.Security;
using Application.Features.Prescriptions.Commands;
using Application.Features.Prescriptions.Dtos;
using Application.Features.Prescriptions.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Prescription workflow: doctors create and manage their own prescriptions,
/// pharmacists view them. Ownership is enforced server-side (a doctor can only
/// cancel/refill prescriptions they issued). List/Get use a combined
/// View-or-Own policy.
/// </summary>
[Authorize]
[ApiVersion("1.0")]
public sealed class PrescriptionsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Lists prescriptions with pagination, search, sorting and status filtering (own records only for doctors).</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir, status.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [Authorize(Policy = "Prescriptions.ViewOrOwn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListPrescriptionsQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Gets a single prescription with its items (own records only for doctors).</summary>
    /// <param name="id">Prescription id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Prescriptions.ViewOrOwn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => OkResponse(new GetPrescriptionQuery(id), cancellationToken);

    /// <summary>Creates a prescription with its line items.</summary>
    /// <param name="request">Patient, items (medicine variant + quantity) and refill settings.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost]
    [Authorize(Policy = Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] CreatePrescriptionRequest request, CancellationToken cancellationToken)
        => Created(nameof(Get), new { id = Guid.Empty }, new CreatePrescriptionCommand(request), cancellationToken);

    /// <summary>Cancels a prescription (own records only; reject an already-cancelled one).</summary>
    /// <param name="id">Prescription id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        => NoContent(new CancelPrescriptionCommand(id), cancellationToken);

    /// <summary>Registers a refill if the prescription is eligible (refills allowed and not yet exhausted).</summary>
    /// <param name="id">Prescription id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("{id:guid}/refill")]
    [Authorize(Policy = Permissions.Prescriptions.ManageOwn)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Refill(Guid id, CancellationToken cancellationToken)
        => NoContent(new RefillPrescriptionCommand(id), cancellationToken);
}