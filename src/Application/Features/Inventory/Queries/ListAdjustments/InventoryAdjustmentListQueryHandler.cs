using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class InventoryAdjustmentListQueryHandler : IRequestHandler<InventoryAdjustmentListQuery, Result<PagedList<InventoryAdjustmentDto>>>
{
    private readonly IMedicineRepository _repo;

    public InventoryAdjustmentListQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<InventoryAdjustmentDto>>> Handle(
        InventoryAdjustmentListQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _repo.ListAdjustmentsAsync(request, cancellationToken);
        return Result<PagedList<InventoryAdjustmentDto>>.Success(page);
    }
}