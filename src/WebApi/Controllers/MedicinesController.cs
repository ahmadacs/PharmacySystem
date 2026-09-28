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

[Authorize]
[ApiVersion("1.0")]
public sealed class MedicinesController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet]
    [Authorize(Policy = Permissions.Medicines.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Medicines)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListMedicinesQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Medicines.View)]
    [OutputCache(PolicyName = OutputCachePolicies.Medicines)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => OkResponse(new GetMedicineQuery(id), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.Medicines.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateMedicineRequest request, CancellationToken cancellationToken)
        => Created(nameof(Get), new { id = Guid.Empty }, new CreateMedicineCommand(request), cancellationToken);

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.Medicines.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineRequest request, CancellationToken cancellationToken)
    {
        if (request.Id != id)
            return FailureResponse("The id in the URL does not match the id in the request body.", StatusCodes.Status400BadRequest);

        return await NoContent(new UpdateMedicineCommand(request), cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Medicines.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => NoContent(new DeleteMedicineCommand(id), cancellationToken);

    [HttpPost("{id:guid}/batches")]
    [Authorize(Policy = Permissions.Medicines.Update)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> AddBatch(Guid id, [FromBody] AddBatchRequest request, CancellationToken cancellationToken)
        => Created(nameof(Get), new { id }, new AddBatchCommand(request), cancellationToken);

    [HttpPost("{id:guid}/variants")]
    [Authorize(Policy = Permissions.Medicines.Update)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddVariant(Guid id, [FromBody] CreateVariantRequest request, CancellationToken cancellationToken)
    {
        if (request.MedicineId != id)
            return FailureResponse("The id in the URL does not match the id in the request body.", StatusCodes.Status400BadRequest);

        return await Created(nameof(Get), new { id }, new CreateVariantCommand(request), cancellationToken);
    }

    [HttpDelete("batches/{batchId:guid}")]
    [Authorize(Policy = Permissions.Medicines.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteBatch(Guid batchId, CancellationToken cancellationToken)
        => NoContent(new DeleteBatchCommand(batchId), cancellationToken);
}