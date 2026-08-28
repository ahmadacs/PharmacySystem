using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed class ListMedicinesQueryHandler : IRequestHandler<ListMedicinesQuery, PagedResult<MedicineListItemDto>>
{
    private readonly IMedicineRepository _repo;

    public ListMedicinesQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<MedicineListItemDto>> Handle(ListMedicinesQuery request, CancellationToken cancellationToken)
        => _repo.ListAsync(request, cancellationToken);
}