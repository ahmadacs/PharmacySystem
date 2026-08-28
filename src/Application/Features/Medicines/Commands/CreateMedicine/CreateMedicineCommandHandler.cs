using Application.Common.Interfaces;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class CreateMedicineCommandHandler : IRequestHandler<CreateMedicineCommand, Guid>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateMedicineCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _repo.MedicineNameExistsAsync(req.Name, null, cancellationToken))
            throw new ConflictingOperationException($"A medicine named '{req.Name}' already exists.");

        GenericName genericName = await _repo.GetOrCreateGenericNameAsync(req.GenericName, cancellationToken);

        var medicine = req.ToEntity(req.Category, genericName);

        var seenVariants = new HashSet<MedicineVariantRequest>();

        foreach (var variantRequest in req.Variants)
        {
            if (!seenVariants.Add(variantRequest))
                throw new ConflictingOperationException(
                    $"A duplicate variant '{variantRequest.Form} {variantRequest.Strength} {variantRequest.Unit}' was provided in this request.");

            medicine.AddVariant(variantRequest.ToEntity(medicine.Id));
        }

        _repo.Add(medicine);
        await _uow.SaveChangesAsync(cancellationToken);

        return medicine.Id;
    }
}