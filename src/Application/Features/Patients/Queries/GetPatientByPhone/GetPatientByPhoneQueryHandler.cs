using Application.Common.Interfaces;
using Application.Common.Models;
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

        var selector = (System.Linq.Expressions.Expression<Func<Patient, PatientRow>>)(p => new PatientRow(
            p.Id,
            p.FirstName,
            p.LastName,
            p.DateOfBirth,
            p.PhoneNumber));

        var row = await _patients.GetAsync(selector, p => p.PhoneNumber == normalized, cancellationToken);
        return Result<PatientDto?>.Success(row?.ToDto());
    }
}
