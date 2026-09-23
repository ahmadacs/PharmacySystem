using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Patients.Dtos;
using Domain.Entities.Patients;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientByPhone;

public sealed class GetPatientByPhoneQueryHandler : IRequestHandler<GetPatientByPhoneQuery, Result<PatientDto?>>
{
    private readonly IBaseRepository<Patient> _patients;
    public GetPatientByPhoneQueryHandler(IBaseRepository<Patient> patients) => _patients = patients;

    public async Task<Result<PatientDto?>> Handle(GetPatientByPhoneQuery request, CancellationToken cancellationToken)
    {
        var normalized = Domain.Common.PhoneNumbers.NormalizeSaudiPhone(request.PhoneNumber);
        var phoneSpec = new Specification<Patient, Patient>(p => p);
        phoneSpec.Where(p => p.PhoneNumber == normalized);
        var patient = await _patients.GetAsync(phoneSpec, cancellationToken);
        return Result<PatientDto?>.Success(patient?.ToDto());
    }
}
