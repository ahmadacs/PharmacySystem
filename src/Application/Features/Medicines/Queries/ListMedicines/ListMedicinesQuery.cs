using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed record ListMedicinesQuery : PagedQuery, IRequest<PagedList<MedicineListItemDto>>
{
    public override string? SortBy { get; init; } = "name";

    public override string SortDir { get; init; } = "asc";

    public int? CategoryId { get; init; }

    public Domain.Enums.MedicineForm? Form { get; init; }

    public bool? IsActive { get; init; }
}
