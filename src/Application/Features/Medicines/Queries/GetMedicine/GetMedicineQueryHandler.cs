using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Queries;

public sealed class GetMedicineQueryHandler : IRequestHandler<GetMedicineQuery, Result<MedicineDetailsDto>>
{
    private readonly IMedicineRepository _repo;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetMedicineQueryHandler(IMedicineRepository repo, IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _localizer = localizer;
    }

    public async Task<Result<MedicineDetailsDto>> Handle(GetMedicineQuery request, CancellationToken cancellationToken)
    {
        var medicine = await _repo.GetByIdWithVariantsAsync(request.Id, cancellationToken);
        if (medicine is null)
            return Result<MedicineDetailsDto>.Failure(_localizer["ResourceNotFound", "Medicine", request.Id].Value);

        return Result<MedicineDetailsDto>.Success(medicine.ToDetailsDto(DateOnly.FromDateTime(DateTime.UtcNow)));
    }
}