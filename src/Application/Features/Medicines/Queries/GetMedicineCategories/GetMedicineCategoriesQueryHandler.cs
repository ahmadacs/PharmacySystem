using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed class GetMedicineCategoriesQueryHandler : IRequestHandler<GetMedicineCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    public Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetMedicineCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = Enum.GetValues<CategoryEnum>()
            .Select(c => new CategoryDto((int)c, c.ToDisplayValue(), null))
            .ToList();
        return Task.FromResult(Result<IReadOnlyList<CategoryDto>>.Success(categories));
    }
}