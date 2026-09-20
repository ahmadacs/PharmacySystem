using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed record ListLowStockQuery : PagedQuery, IRequest<Result<PagedList<LowStockDto>>>
{
    public override string? SortBy { get; init; } = "medicineName";

    public override string SortDir { get; init; } = "asc";
}
