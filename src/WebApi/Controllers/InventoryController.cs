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

[Authorize]
[ApiVersion("1.0")]
public sealed class InventoryController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet("batches")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Batches(
        [FromQuery] BatchListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpGet("summary")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Summary(
        [FromQuery] MedicineInventorySummaryQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpGet("expiry-alerts")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ExpiryAlerts(
        [FromQuery] ExpiryAlertListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpGet("low-stock")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Inventory)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> LowStock(
        [FromQuery] ListLowStockQuery query,
        CancellationToken cancellationToken)
        => OkResponse(query, cancellationToken);

    [HttpGet("adjustments")]
    [Authorize(Policy = Permissions.Inventory.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Adjustments(
        [FromQuery] InventoryAdjustmentListQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpPost("adjustments")]
    [Authorize(Policy = Permissions.Inventory.Adjust)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Adjust([FromBody] AdjustInventoryRequest request, CancellationToken cancellationToken)
        => Created(nameof(Adjustments), new { id = Guid.Empty }, new AdjustInventoryCommand(request), cancellationToken);

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