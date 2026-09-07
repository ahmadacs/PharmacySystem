using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using MediatR;

namespace Application.Features.Dispensing.Queries;

public sealed record DispensingRecordListQuery : PagedQuery, IRequest<Result<PagedList<DispensingRecordDto>>>
{
    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public override string? SortBy { get; init; } = "dispensedAt";
}
