using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ExpiryAlertListQueryHandler : IRequestHandler<ExpiryAlertListQuery, Result<PagedList<ExpiryAlertDto>>>
{
    private readonly IMedicineRepository _repo;

    public ExpiryAlertListQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<ExpiryAlertDto>>> Handle(ExpiryAlertListQuery request, CancellationToken cancellationToken)
    {
        var page = await _repo.ListExpiryAlertsAsync(request, cancellationToken);
        return Result<PagedList<ExpiryAlertDto>>.Success(page);
    }
}