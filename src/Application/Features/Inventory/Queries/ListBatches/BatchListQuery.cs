using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

/// <summary>Lists medicine batches with expiry awareness (All / Valid / ExpiringSoon / Expired).</summary>
public sealed record BatchListQuery : PagedQuery, IRequest<Result<PagedList<MedicineBatchDto>>>
{
    public Guid? MedicineId { get; init; }

    [StringLength(20, ErrorMessage = "ExpiryStatus must be at most 20 characters.")]
    public string? ExpiryStatus { get; init; } = "All";

    [Range(1, 365, ErrorMessage = "WithinDays must be between 1 and 365.")]
    public int WithinDays { get; init; } = 30;

    public override string? SortBy { get; init; } = "expiryDate";

    public override string SortDir { get; init; } = "asc";
}
