using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Entities.Prescriptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Queries;

public sealed class GetPrescriptionQueryHandler : IRequestHandler<GetPrescriptionQuery, Result<PrescriptionDetailsDto>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IStaffService _staff;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetPrescriptionQueryHandler(
        IBaseRepository<Prescription> prescriptions,
        IStaffService staff,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _staff = staff;
        _localizer = localizer;
    }

    public async Task<Result<PrescriptionDetailsDto>> Handle(GetPrescriptionQuery request, CancellationToken cancellationToken)
    {

        var row = await _prescriptions.GetReadAsync(PrescriptionProjections.ToDetailsRow, p => p.Id == request.Id, cancellationToken);
        if (row is null)
            return Result<PrescriptionDetailsDto>.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

        var doctorName = await _staff.GetDoctorNameAsync(row.DoctorId, cancellationToken) ?? string.Empty;

        return Result<PrescriptionDetailsDto>.Success(row.ToDetailsDto(doctorName));
    }
}
