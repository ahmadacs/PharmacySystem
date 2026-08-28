using Application.Common.Interfaces;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class UpdateMedicineCommandHandler : IRequestHandler<UpdateMedicineCommand, Unit>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateMedicineCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Unit> Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var medicine = await _repo.GetByIdAsync(req.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Medicine), req.Id);

        if (await _repo.MedicineNameExistsAsync(req.Name, req.Id, cancellationToken))
            throw new ConflictingOperationException($"A medicine named '{req.Name}' already exists.");

        var genericName = await _repo.GetOrCreateGenericNameAsync(req.GenericName, cancellationToken);

        medicine.UpdateDetails(
            req.Name,
            req.Category,
            req.ReorderLevel,
            genericName,
            req.IsControlled,
            req.NameAr);

        if (req.IsActive) medicine.Activate();
        else medicine.Deactivate();

        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}