using Application.Common.Models;
using Application.Features.Patients.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientPrescriptions;

public sealed record GetPatientPrescriptionsQuery(Guid PatientId, int LookbackDays = 180)
    : IRequest<Result<IReadOnlyList<PatientPrescriptionHistoryDto>>>;
