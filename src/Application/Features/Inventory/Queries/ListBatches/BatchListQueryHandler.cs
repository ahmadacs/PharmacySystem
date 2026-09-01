using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class BatchListQueryHandler : IRequestHandler<BatchListQuery, Result<PagedList<MedicineBatchDto>>>
{
    private readonly IMedicineRepository _repo;

    public BatchListQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<MedicineBatchDto>>> Handle(BatchListQuery request, CancellationToken cancellationToken)
    {
        var page = await _repo.ListBatchesAsync(request, cancellationToken);
        return Result<PagedList<MedicineBatchDto>>.Success(page);
    }
}