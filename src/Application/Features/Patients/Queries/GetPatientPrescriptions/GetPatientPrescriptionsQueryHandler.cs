using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientPrescriptions;

public sealed class GetPatientPrescriptionsQueryHandler : IRequestHandler<GetPatientPrescriptionsQuery, Result<IReadOnlyList<PrescriptionListItemDto>>>
{
    private readonly IPrescriptionRepository _prescriptions;

    public GetPatientPrescriptionsQueryHandler(IPrescriptionRepository prescriptions) => _prescriptions = prescriptions;

    public async Task<Result<IReadOnlyList<PrescriptionListItemDto>>> Handle(GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var list = await _prescriptions.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result<IReadOnlyList<PrescriptionListItemDto>>.Success(list.Select(p => p.ToListItemDto(p.DoctorId.ToString()[..8])).ToList());
    }
}
