using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

/// <summary>
/// Expiry alerts derived from the batches. Status values: All / Critical
/// (expires within 30 days) / Warning (within 90 days) / Safe / Expired.
/// Days remaining is computed with UTC "today" (see the query handler).
/// </summary>
public sealed record ExpiryAlertListQuery : PagedQuery, IRequest<Result<PagedList<ExpiryAlertDto>>>
{
    [StringLength(20, ErrorMessage = "Status must be at most 20 characters.")]
    public string? Status { get; init; } = "All";

    public override string? SortBy { get; init; } = "expiryDate";

    public override string SortDir { get; init; } = "asc";
}
