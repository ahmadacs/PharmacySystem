using Application.Common.Security;
using Application.Features.Dispensing.Commands;
using Application.Features.Dispensing.Dtos;
using Application.Features.Dispensing.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Dispensing operations: pharmacists view dispensing history and dispense
/// prescriptions. Dispensing validates the prescription and its batches, selects
/// the first-to-expire non-expired batches, checks sufficient stock and reduces
/// it atomically in a single transaction.
/// </summary>
[Authorize]
[ApiVersion("1.0")]
public sealed class DispensingController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Lists dispensing records with pagination, search and sorting.</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [Authorize(Policy = Permissions.Dispensing.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] DispensingRecordListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Dispenses a prescription: validates stock and expiry, reduces batches atomically, records the transaction.</summary>
    /// <param name="request">Prescription id and optional notes.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost]
    [Authorize(Policy = Permissions.Dispensing.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> Dispense([FromBody] DispensePrescriptionRequest request, CancellationToken cancellationToken)
        => Created(nameof(List), new { id = Guid.Empty }, new DispensePrescriptionCommand(request), cancellationToken);
}