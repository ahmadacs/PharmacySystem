using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed record InventoryAdjustmentListQuery(
    [param: Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    int Page = 1,
    [param: Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    int PageSize = 10,
    [param: StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    string? Search = null,
    Domain.Enums.InventoryAdjustmentType? Type = null,
    [param: StringLength(50, ErrorMessage = "SortBy must be at most 50 characters.")]
    string? SortBy = "adjustedAt",
    [param: RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
    string SortDir = "desc") : IRequest<PagedResult<InventoryAdjustmentDto>>;