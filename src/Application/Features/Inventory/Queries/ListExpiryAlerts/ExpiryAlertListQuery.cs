using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed record ExpiryAlertListQuery : PagedQuery, IRequest<Result<PagedList<ExpiryAlertDto>>>
{
    [StringLength(20, ErrorMessage = "Status must be at most 20 characters.")]
    public string? Status { get; init; } = "All";

    public override string? SortBy { get; init; } = "expiryDate";

    public override string SortDir { get; init; } = "asc";
}
