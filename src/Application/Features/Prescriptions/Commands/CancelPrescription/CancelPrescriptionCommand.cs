using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed record CancelPrescriptionCommand(Guid Id) : IRequest<Result>, IOwnedPrescriptionRequest
{
    public Guid PrescriptionId => Id;
    public PrescriptionOperation Operation => PrescriptionOperation.Manage;
}