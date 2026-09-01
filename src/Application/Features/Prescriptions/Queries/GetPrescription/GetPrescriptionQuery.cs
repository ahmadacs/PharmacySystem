using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Prescriptions.Queries;

public sealed record GetPrescriptionQuery(Guid Id)
    : IRequest<Result<PrescriptionDetailsDto>>, IOwnedPrescriptionRequest
{
    public Guid PrescriptionId => Id;
    public PrescriptionOperation Operation => PrescriptionOperation.View;
}