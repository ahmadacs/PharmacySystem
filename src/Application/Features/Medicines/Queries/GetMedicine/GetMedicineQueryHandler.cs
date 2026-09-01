using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed class GetMedicineQueryHandler : IRequestHandler<GetMedicineQuery, Result<MedicineDetailsDto>>
{
    private readonly IMedicineRepository _repo;

    public GetMedicineQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<MedicineDetailsDto>> Handle(GetMedicineQuery request, CancellationToken cancellationToken)
    {
        var medicine = await _repo.GetByIdWithVariantsAsync(request.Id, cancellationToken);
        if (medicine is null)
            return Result<MedicineDetailsDto>.Failure($"Medicine not found with id '{request.Id}'.");

        return Result<MedicineDetailsDto>.Success(medicine.ToDetailsDto(DateOnly.FromDateTime(DateTime.UtcNow)));
    }
}