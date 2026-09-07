using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed record InventoryAdjustmentListQuery : PagedQuery, IRequest<Result<PagedList<InventoryAdjustmentDto>>>
{
    public Domain.Enums.InventoryAdjustmentType? Type { get; init; }

    public override string? SortBy { get; init; } = "adjustedAt";
}
