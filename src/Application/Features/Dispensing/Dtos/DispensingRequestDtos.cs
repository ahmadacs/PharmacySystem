using System.ComponentModel.DataAnnotations;
using Application.Common.Attributes;

namespace Application.Features.Dispensing.Dtos;

public sealed record DispensePrescriptionRequest
{
    [Required]
    [StringLength(8, MinimumLength = 6)]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "The prescription code must be alphanumeric.")]
    public string ShortCode { get; init; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 9)]
    [SaudiPhone]
    public string PhoneNumber { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; init; }
}