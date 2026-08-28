using Application.Features.Medicines.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed class GetMedicineCategoriesQueryHandler : IRequestHandler<GetMedicineCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public Task<IReadOnlyList<CategoryDto>> Handle(GetMedicineCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = Enum.GetValues<CategoryEnum>()
            .Select(c => new CategoryDto((int)c, c.ToDisplayValue(), null))
            .ToList();
        return Task.FromResult<IReadOnlyList<CategoryDto>>(categories);
    }
}