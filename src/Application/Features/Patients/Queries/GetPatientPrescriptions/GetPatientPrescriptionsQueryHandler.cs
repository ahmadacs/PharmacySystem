using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientPrescriptions;

public sealed class GetPatientPrescriptionsQueryHandler : IRequestHandler<GetPatientPrescriptionsQuery, Result<IReadOnlyList<PrescriptionListItemDto>>>
{
    private readonly IPatientRepository _patients;

    public GetPatientPrescriptionsQueryHandler(IPatientRepository patients) => _patients = patients;

    public async Task<Result<IReadOnlyList<PrescriptionListItemDto>>> Handle(GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var prescriptions = await _patients.GetPrescriptionsAsync(request.PatientId, cancellationToken);
        return Result<IReadOnlyList<PrescriptionListItemDto>>.Success(prescriptions);
    }
}
