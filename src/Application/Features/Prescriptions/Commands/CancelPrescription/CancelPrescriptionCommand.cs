using Application.Features.Prescriptions.Common;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed record CancelPrescriptionCommand(Guid Id) : IRequest, IOwnedPrescriptionRequest
{
    public Guid PrescriptionId => Id;
    public PrescriptionOperation Operation => PrescriptionOperation.Manage;
}