using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class BatchListQueryHandler : IRequestHandler<BatchListQuery, PagedResult<MedicineBatchDto>>
{
    private readonly IMedicineRepository _repo;

    public BatchListQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<MedicineBatchDto>> Handle(BatchListQuery request, CancellationToken cancellationToken)
        => _repo.ListBatchesAsync(request, cancellationToken);
}