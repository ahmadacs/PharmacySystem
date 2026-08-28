using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

/// <summary>Lists medicine batches with expiry awareness (All / Valid / ExpiringSoon / Expired).</summary>
public sealed record BatchListQuery(
    [param: Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    int Page = 1,
    [param: Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    int PageSize = 10,
    [param: StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    string? Search = null,
    Guid? MedicineId = null,
    [param: StringLength(20, ErrorMessage = "ExpiryStatus must be at most 20 characters.")]
    string? ExpiryStatus = "All",
    [param: Range(1, 365, ErrorMessage = "WithinDays must be between 1 and 365.")]
    int WithinDays = 30,
    [param: StringLength(50, ErrorMessage = "SortBy must be at most 50 characters.")]
    string? SortBy = "expiryDate",
    [param: RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
    string SortDir = "asc") : IRequest<PagedResult<MedicineBatchDto>>;