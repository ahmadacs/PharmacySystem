using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

/// <summary>
/// Expiry alerts derived from the batches. Status values: All / Critical
/// (expires within 30 days) / Warning (within 90 days) / Safe / Expired.
/// Days remaining is computed with UTC "today" (see the repository).
/// </summary>
public sealed record ExpiryAlertListQuery(
    [param: Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    int Page = 1,
    [param: Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    int PageSize = 10,
    [param: StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    string? Search = null,
    [param: StringLength(20, ErrorMessage = "Status must be at most 20 characters.")]
    string? Status = "All",
    [param: StringLength(50, ErrorMessage = "SortBy must be at most 50 characters.")]
    string? SortBy = "expiryDate",
    [param: RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
    string SortDir = "asc") : IRequest<Result<PagedList<ExpiryAlertDto>>>;