using Application.Common.Interfaces;
using Application.Features.Patients.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientByPhone;

public sealed class GetPatientByPhoneQueryHandler : IRequestHandler<GetPatientByPhoneQuery, PatientDto?>
{
    private readonly IPatientRepository _patients;
    public GetPatientByPhoneQueryHandler(IPatientRepository patients) => _patients = patients;

    public async Task<PatientDto?> Handle(GetPatientByPhoneQuery request, CancellationToken cancellationToken)
    {
        var normalized = request.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
        var patient = await _patients.FindByPhoneAsync(normalized, cancellationToken);
        return patient?.ToDto();
    }
}
