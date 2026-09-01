using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using MediatR;

namespace Application.Features.Dispensing.Queries;

public sealed class DispensingRecordListQueryHandler : IRequestHandler<DispensingRecordListQuery, Result<PagedList<DispensingRecordDto>>>
{
    private readonly IPrescriptionRepository _prescriptions;

    public DispensingRecordListQueryHandler(IPrescriptionRepository prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public async Task<Result<PagedList<DispensingRecordDto>>> Handle(
        DispensingRecordListQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _prescriptions.ListDispensingRecordsAsync(request, cancellationToken);
        return Result<PagedList<DispensingRecordDto>>.Success(page);
    }
}