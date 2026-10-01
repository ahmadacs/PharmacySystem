using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Queries;

public sealed class GetMedicineQueryHandler : IRequestHandler<GetMedicineQuery, Result<MedicineDetailsDto>>
{
    private readonly IBaseRepository<Medicine> _repo;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetMedicineQueryHandler(
        IBaseRepository<Medicine> repo,
        IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _localizer = localizer;
    }

    public async Task<Result<MedicineDetailsDto>> Handle(GetMedicineQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var row = await _repo.GetReadAsync(MedicineProjections.ToDetailsRow(asOf), m => m.Id == request.Id, cancellationToken);
        if (row is null)
            return Result<MedicineDetailsDto>.Failure(_localizer["ResourceNotFound", "Medicine", request.Id].Value, 404);

        return Result<MedicineDetailsDto>.Success(row.ToDto(asOf));
    }
}
