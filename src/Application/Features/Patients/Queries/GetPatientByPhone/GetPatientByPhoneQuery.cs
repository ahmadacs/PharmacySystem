using Application.Features.Patients.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientByPhone;

public sealed record GetPatientByPhoneQuery(string PhoneNumber) : IRequest<PatientDto?>;
