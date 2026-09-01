using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

/// <summary>
/// Per-medicine aggregated inventory rows. Stock status values: All / InStock /
/// LowStock / OutOfStock. Every quantity is computed from the active batches.
/// </summary>
public sealed record MedicineInventorySummaryQuery(
    [param: Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    int Page = 1,
    [param: Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    int PageSize = 10,
    [param: StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    string? Search = null,
    [param: StringLength(20, ErrorMessage = "StockStatus must be at most 20 characters.")]
    string? StockStatus = "All",
    [param: StringLength(50, ErrorMessage = "SortBy must be at most 50 characters.")]
    string? SortBy = "name",
    [param: RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
    string SortDir = "asc") : IRequest<PagedList<MedicineInventorySummaryDto>>;