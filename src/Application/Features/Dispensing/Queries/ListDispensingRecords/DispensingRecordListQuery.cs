using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using MediatR;

namespace Application.Features.Dispensing.Queries;

public sealed record DispensingRecordListQuery(
    [param: Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    int Page = 1,
    [param: Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    int PageSize = 10,
    [param: StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    [param: StringLength(50, ErrorMessage = "SortBy must be at most 50 characters.")]
    string? SortBy = "dispensedAt",
    [param: RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
    string SortDir = "desc") : IRequest<Result<PagedList<DispensingRecordDto>>>;