using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ListLowStockQueryHandler : IRequestHandler<ListLowStockQuery, Result<IReadOnlyList<LowStockDto>>>
{
    private readonly IMedicineRepository _repo;

    public ListLowStockQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<IReadOnlyList<LowStockDto>>> Handle(ListLowStockQuery request, CancellationToken cancellationToken)
    {
        var lowStock = await _repo.GetLowStockAsync(cancellationToken);
        return Result<IReadOnlyList<LowStockDto>>.Success(lowStock);
    }
}