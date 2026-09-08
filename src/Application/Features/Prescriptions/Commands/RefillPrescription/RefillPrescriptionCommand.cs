using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

/// <summary>
/// Refills one or more items of a prescription. Single-item refills pass a
/// one-element <see cref="ItemIds"/>; batch refills pass several.
/// </summary>
public sealed record RefillPrescriptionCommand(
    Guid Id,
    [MinLength(1)] List<Guid> ItemIds) : IRequest<Result>, IOwnedPrescriptionRequest
{
    public Guid PrescriptionId => Id;
    public PrescriptionOperation Operation => PrescriptionOperation.Manage;
}

/// <summary>Body for POST /api/v1/prescriptions/{id}/refill (batch refill).</summary>
public sealed record RefillPrescriptionRequest
{
    [Required, MinLength(1)]
    public List<Guid> ItemIds { get; init; } = [];
}
