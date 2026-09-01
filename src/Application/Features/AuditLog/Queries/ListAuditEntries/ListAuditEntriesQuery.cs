using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed record ListAuditEntriesQuery(
    [param: StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    string? Search = null,
    [param: EnumDataType(typeof(AuditAction), ErrorMessage = "Action must be Created, Updated or Deleted.")]
    AuditAction? Action = null,
    [param: StringLength(200, ErrorMessage = "Entity must be at most 200 characters.")]
    string? Entity = null,
    DateTime? From = null,
    DateTime? To = null,
    [param: Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    int Page = 1,
    [param: Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    int PageSize = 10,
    [param: StringLength(50, ErrorMessage = "SortBy must be at most 50 characters.")]
    string? SortBy = "changedAt",
    [param: RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
    string SortDir = "desc") : IRequest<Result<PagedList<AuditEntryDto>>>;