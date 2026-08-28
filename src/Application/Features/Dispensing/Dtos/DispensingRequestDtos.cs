using System.ComponentModel.DataAnnotations;

namespace Application.Features.Dispensing.Dtos;

public sealed record DispensePrescriptionRequest
{
    public Guid PrescriptionId { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}