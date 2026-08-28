using Application.Common.Security;
using Application.Features.Inventory.Commands;
using Application.Features.Inventory.Dtos;
using Application.Features.Inventory.Queries;
using Application.Features.Medicines.Commands;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using WebApi.Caching;

namespace WebApi.Controllers;

/// <summary>
/// Inventory operations: batch visibility with expiry status, a per-medicine
/// stock summary, expiry alerts, low-stock list and atomic stock adjustments.
/// Viewing requires Permissions.Inventory.View; adjusting requires
/// Permissions.Inventory.Adjust.
/// </summary>
[Authorize]
[ApiVersion("1.0")]
public sealed class InventoryController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Lists medicine batches with pagination, search, sorting and expiry-status filtering.</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir, expiryStatus, withinDays.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("batches")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Batches(
        [FromQuery] BatchListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Summarises stock per medicine (total quantity, status, nearest expiry, active batches).</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir, stockStatus.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("summary")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Summary(
        [FromQuery] MedicineInventorySummaryQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Lists batches approaching or past their expiry date with a status level.</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir, status.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("expiry-alerts")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ExpiryAlerts(
        [FromQuery] ExpiryAlertListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Returns medicines whose available stock is at or below their reorder level.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("low-stock")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> LowStock(CancellationToken cancellationToken)
        => OkResponse(new ListLowStockQuery(), cancellationToken);

    /// <summary>Lists stock adjustments with pagination, search, sorting and type filtering.</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir, type.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("adjustments")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Adjustments(
        [FromQuery] InventoryAdjustmentListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Records a stock adjustment (increase/decrease/damaged/expired/etc.) and updates the batch quantity atomically.</summary>
    /// <param name="request">Batch id, adjustment type, quantity and reason.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("adjustments")]
    [Authorize(Policy = Permissions.Inventory.Adjust)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Adjust([FromBody] AdjustInventoryRequest request, CancellationToken cancellationToken)
        => Created(nameof(Adjustments), new { id = Guid.Empty }, new AdjustInventoryCommand(request), cancellationToken);

    /// <summary>Receives a brand-new batch into stock (Increase/TransferIn) and records an Increase adjustment with a mandatory reason.</summary>
    /// <param name="request">Variant, batch details, packages received and the required reason.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("receive")]
    [Authorize(Policy = Permissions.Inventory.Adjust)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Receive([FromBody] ReceiveInventoryRequest request, CancellationToken cancellationToken)
        => Created(nameof(Adjustments), new { id = Guid.Empty },
            new AddBatchCommand(request.ToAddBatchRequest(), request.Reason), cancellationToken);
}