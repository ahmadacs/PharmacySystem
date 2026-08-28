using Application.Common.Interfaces;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ListLowStockQueryHandler : IRequestHandler<ListLowStockQuery, IReadOnlyList<LowStockDto>>
{
    private readonly IMedicineRepository _repo;

    public ListLowStockQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<LowStockDto>> Handle(ListLowStockQuery request, CancellationToken cancellationToken)
        => _repo.GetLowStockAsync(cancellationToken);
}