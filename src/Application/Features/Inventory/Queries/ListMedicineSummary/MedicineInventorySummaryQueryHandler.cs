using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class MedicineInventorySummaryQueryHandler
    : IRequestHandler<MedicineInventorySummaryQuery, PagedList<MedicineInventorySummaryDto>>
{
    private readonly IMedicineRepository _repo;

    public MedicineInventorySummaryQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedList<MedicineInventorySummaryDto>> Handle(
        MedicineInventorySummaryQuery request,
        CancellationToken cancellationToken)
        => _repo.ListMedicineSummaryAsync(request, cancellationToken);
}