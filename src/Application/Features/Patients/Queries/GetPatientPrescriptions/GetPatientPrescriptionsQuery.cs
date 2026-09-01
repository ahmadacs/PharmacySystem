using Application.Common.Models;
using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientPrescriptions;

public sealed record GetPatientPrescriptionsQuery(Guid PatientId) : IRequest<Result<IReadOnlyList<PrescriptionListItemDto>>>;
