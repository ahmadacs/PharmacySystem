using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class UpdateMedicineCommandHandler : IRequestHandler<UpdateMedicineCommand, Result>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateMedicineCommandHandler(IMedicineRepository repo, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var medicine = await _repo.GetByIdAsync(req.Id, cancellationToken);
        if (medicine is null)
            return Result.Failure(_localizer["ResourceNotFound", "Medicine", req.Id].Value, 404);

        if (await _repo.MedicineNameExistsAsync(req.Name, req.Id, cancellationToken))
            return Result.Failure(_localizer["MedicineAlreadyExists", req.Name].Value, 409);

        var genericName = await MedicineMapping.ResolveGenericNameAsync(_repo, req.GenericName, req.GenericNameAr, cancellationToken);

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