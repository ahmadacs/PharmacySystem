using Application.Common.Security;
using Application.Features.Medicines.Commands;
using Application.Features.Medicines.Dtos;
using Application.Features.Medicines.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using WebApi.Caching;

namespace WebApi.Controllers;

/// <summary>
/// Medicine catalogue management: list, create, update, delete medicines and
/// their batches/variants. Read operations require Permissions.Medicines.View;
/// writes require the Create/Update/Delete permissions.
/// </summary>
[Authorize]
[ApiVersion("1.0")]
public sealed class MedicinesController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Lists medicines with pagination, search, sorting and filters.</summary>
    /// <param name="query">Page, pageSize, search, sortBy, sortDir, category, form, status.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [Authorize(Policy = Permissions.Medicines.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Medicines)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListMedicinesQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    /// <summary>Gets a medicine's details including its variants and batches.</summary>
    /// <param name="id">Medicine id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Medicines.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Medicines)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => OkResponse(new GetMedicineQuery(id), cancellationToken);

    /// <summary>Lists the therapeutic categories used by the catalogue.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("categories")]
    [Authorize(Policy = Permissions.Medicines.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Medicines)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Categories(CancellationToken cancellationToken)
        => OkResponse(new GetMedicineCategoriesQuery(), cancellationToken);

    /// <summary>Creates a medicine with its initial variants.</summary>
    /// <param name="request">Medicine name, generic name, category and variants.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost]
    [Authorize(Policy = Permissions.Medicines.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateMedicineRequest request, CancellationToken cancellationToken)
        => Created(nameof(Get), new { id = Guid.Empty }, new CreateMedicineCommand(request), cancellationToken);

    /// <summary>Updates a medicine's catalogue data.</summary>
    /// <param name="id">Medicine id (must match the body id).</param>
    /// <param name="request">Updated medicine fields.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.Medicines.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineRequest request, CancellationToken cancellationToken)
    {
        if (request.Id != id)
            return BadRequest("The id in the URL does not match the id in the request body.");

        return await NoContent(new UpdateMedicineCommand(request), cancellationToken);
    }

    /// <summary>Soft-deletes a medicine.</summary>
    /// <param name="id">Medicine id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Medicines.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => NoContent(new DeleteMedicineCommand(id), cancellationToken);

    /// <summary>Adds a batch with an expiry date to a medicine variant.</summary>
    /// <param name="id">Medicine id.</param>
    /// <param name="request">Variant, batch number, expiry/manufacture dates and quantity.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("{id:guid}/batches")]
    [Authorize(Policy = Permissions.Medicines.Update)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> AddBatch(Guid id, [FromBody] AddBatchRequest request, CancellationToken cancellationToken)
        => Created(nameof(Get), new { id }, new AddBatchCommand(request), cancellationToken);

    /// <summary>Adds a variant (form/strength/unit) to a medicine.</summary>
    /// <param name="id">Medicine id (must match the body medicineId).</param>
    /// <param name="request">Variant form, strength, unit and activation flag.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("{id:guid}/variants")]
    [Authorize(Policy = Permissions.Medicines.Update)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddVariant(Guid id, [FromBody] CreateVariantRequest request, CancellationToken cancellationToken)
    {
        if (request.MedicineId != id)
            return BadRequest("The id in the URL does not match the id in the request body.");

        return await Created(nameof(Get), new { id }, new CreateVariantCommand(request), cancellationToken);
    }

    /// <summary>Soft-deletes a batch.</summary>
    /// <param name="batchId">Batch id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpDelete("batches/{batchId:guid}")]
    [Authorize(Policy = Permissions.Medicines.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteBatch(Guid batchId, CancellationToken cancellationToken)
        => NoContent(new DeleteBatchCommand(batchId), cancellationToken);
}