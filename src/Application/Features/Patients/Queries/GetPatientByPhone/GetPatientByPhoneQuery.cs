using Application.Common.Models;
using Application.Features.Patients.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientByPhone;

public sealed record GetPatientByPhoneQuery(string PhoneNumber) : IRequest<Result<PatientDto?>>;
