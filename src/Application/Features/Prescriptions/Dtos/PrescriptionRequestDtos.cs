using System.ComponentModel.DataAnnotations;
using Application.Common.Attributes;

namespace Application.Features.Prescriptions.Dtos;

public sealed record PrescriptionItemRequest
{
    public Guid MedicineVariantId { get; init; }

    [PositiveQuantity]
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [StringLength(300)]
    public string? DosageInstructions { get; init; }
}

public sealed record CreatePrescriptionRequest
{
    [Required, StringLength(100)]
    public string PatientFirstName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string PatientLastName { get; init; } = string.Empty;

    [Required]
    [NotInTheFuture]
    public DateOnly PatientDateOfBirth { get; init; }

    [Required]
    [StringLength(30)]
    [SaudiPhone]
    public string PatientPhoneNumber { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Diagnosis { get; init; }

    [NotInTheFuture]
    public DateOnly IssuedDate { get; init; }

    public bool IsRefillable { get; init; }

    [Range(0, 99)]
    public int RefillsAllowed { get; init; }

    [MinLength(1)]
    public List<PrescriptionItemRequest> Items { get; init; } = [];
}