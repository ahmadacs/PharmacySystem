using Application.Common.Interfaces;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed class GetMedicineQueryHandler : IRequestHandler<GetMedicineQuery, MedicineDetailsDto>
{
    private readonly IMedicineRepository _repo;

    public GetMedicineQueryHandler(IMedicineRepository repo)
    {
        _repo = repo;
    }

    public async Task<MedicineDetailsDto> Handle(GetMedicineQuery request, CancellationToken cancellationToken)
    {
        var medicine = await _repo.GetByIdWithVariantsAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Medicine), request.Id);

        return medicine.ToDetailsDto(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}