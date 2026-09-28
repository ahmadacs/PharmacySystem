using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed record RefillPrescriptionCommand(
    Guid Id,
    [MinLength(1)] List<Guid> ItemIds) : IRequest<Result>, IOwnedPrescriptionRequest
{
    public Guid PrescriptionId => Id;
    public PrescriptionOperation Operation => PrescriptionOperation.Manage;
}

public sealed record RefillPrescriptionRequest
{
    [Required, MinLength(1)]
    public List<Guid> ItemIds { get; init; } = [];
}
