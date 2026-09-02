using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class UpdateMedicineCommandHandler : IRequestHandler<UpdateMedicineCommand, Result>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateMedicineCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var medicine = await _repo.GetByIdAsync(req.Id, cancellationToken);
        if (medicine is null)
            return Result.Failure($"Resource 'Medicine' with id '{req.Id}' was not found.", 404);

        if (await _repo.MedicineNameExistsAsync(req.Name, req.Id, cancellationToken))
            return Result.Failure($"A medicine named '{req.Name}' already exists.", 409);

        var genericName = await _repo.GetOrCreateGenericNameAsync(req.GenericName, req.GenericNameAr, cancellationToken);

        medicine.UpdateDetails(
            req.Name,
            req.Category,
            genericName,
            req.IsControlled,
            req.NameAr);

        if (req.IsActive) medicine.Activate();
        else medicine.Deactivate();

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, 422);
        }

        return Result.Success();
    }
}