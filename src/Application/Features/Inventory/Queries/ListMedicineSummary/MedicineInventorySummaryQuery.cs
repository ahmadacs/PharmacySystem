using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

/// <summary>
/// Per-medicine aggregated inventory rows. Stock status values: All / InStock /
/// LowStock / OutOfStock. Every quantity is computed from the active batches.
/// </summary>
public sealed record MedicineInventorySummaryQuery : PagedQuery, IRequest<PagedList<MedicineInventorySummaryDto>>
{
    [StringLength(20, ErrorMessage = "StockStatus must be at most 20 characters.")]
    public string? StockStatus { get; init; } = "All";

    public override string? SortBy { get; init; } = "name";

    public override string SortDir { get; init; } = "asc";
}
