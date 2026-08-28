using Application.Common.Interfaces;
using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientPrescriptions;

public sealed class GetPatientPrescriptionsQueryHandler : IRequestHandler<GetPatientPrescriptionsQuery, IReadOnlyList<PrescriptionListItemDto>>
{
    private readonly IPatientRepository _patients;

    public GetPatientPrescriptionsQueryHandler(IPatientRepository patients) => _patients = patients;

    public Task<IReadOnlyList<PrescriptionListItemDto>> Handle(GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
        => _patients.GetPrescriptionsAsync(request.PatientId, cancellationToken);
}
