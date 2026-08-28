using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed record CreatePrescriptionCommand(CreatePrescriptionRequest Request) : IRequest<Guid>;