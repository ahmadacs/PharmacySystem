using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed record GetMedicineCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;