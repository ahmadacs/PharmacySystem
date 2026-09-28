using System.ComponentModel.DataAnnotations;
using Application.Common.Attributes;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using MediatR;

namespace Application.Features.Dispensing.Queries;

public sealed record LookupPrescriptionForDispensingQuery : IRequest<Result<DispensingLookupResponse>>
{
    [Required]
    [StringLength(8, MinimumLength = 6)]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "The prescription code must be alphanumeric.")]
    public string ShortCode { get; init; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 9)]
    [SaudiPhone]
    public string PhoneNumber { get; init; } = string.Empty;
}
