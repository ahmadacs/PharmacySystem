using Application.Common.Models;
using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Prescriptions.Queries;

public sealed record ListPrescriptionsQuery : PagedQuery, IRequest<Result<PagedList<PrescriptionListItemDto>>>
{
    public Domain.Enums.PrescriptionStatus? Status { get; init; }

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public override string? SortBy { get; init; } = "issuedDate";
}
