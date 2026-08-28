using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using MediatR;

namespace Application.Features.Dispensing.Queries;

public sealed class DispensingRecordListQueryHandler : IRequestHandler<DispensingRecordListQuery, PagedResult<DispensingRecordDto>>
{
    private readonly IPrescriptionRepository _prescriptions;

    public DispensingRecordListQueryHandler(IPrescriptionRepository prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public Task<PagedResult<DispensingRecordDto>> Handle(
        DispensingRecordListQuery request,
        CancellationToken cancellationToken)
        => _prescriptions.ListDispensingRecordsAsync(request, cancellationToken);
}