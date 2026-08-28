using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class InventoryAdjustmentListQueryHandler : IRequestHandler<InventoryAdjustmentListQuery, PagedResult<InventoryAdjustmentDto>>
{
    private readonly IMedicineRepository _repo;

    public InventoryAdjustmentListQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<InventoryAdjustmentDto>> Handle(
        InventoryAdjustmentListQuery request,
        CancellationToken cancellationToken)
        => _repo.ListAdjustmentsAsync(request, cancellationToken);
}