using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ExpiryAlertListQueryHandler : IRequestHandler<ExpiryAlertListQuery, PagedResult<ExpiryAlertDto>>
{
    private readonly IMedicineRepository _repo;

    public ExpiryAlertListQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<ExpiryAlertDto>> Handle(ExpiryAlertListQuery request, CancellationToken cancellationToken)
        => _repo.ListExpiryAlertsAsync(request, cancellationToken);
}