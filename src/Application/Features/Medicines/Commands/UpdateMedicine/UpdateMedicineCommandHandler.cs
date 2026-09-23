using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class UpdateMedicineCommandHandler : IRequestHandler<UpdateMedicineCommand, Result>
{
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<GenericName> _generics;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateMedicineCommandHandler(IBaseRepository<Medicine> medicines, IBaseRepository<GenericName> generics, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _medicines = medicines;
        _generics = generics;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var byIdSpec = new Specification<Medicine, Medicine>(m => m).Tracked();
        byIdSpec.Where(m => m.Id == req.Id);
        var medicine = await _medicines.GetAsync(byIdSpec, cancellationToken);
        if (medicine is null)
            return Result.Failure(_localizer["ResourceNotFound", "Medicine", req.Id].Value, 404);

        var nameSpec = new Specification<Medicine, Medicine>(m => m);
        nameSpec.Where(m => m.Name == req.Name.Trim() && m.Id != req.Id);
        if (await _medicines.CountAsync(nameSpec, cancellationToken) > 0)
            return Result.Failure(_localizer["MedicineAlreadyExists", req.Name].Value, 409);

        var genericName = await MedicineMapping.ResolveGenericNameAsync(_generics, req.GenericName, req.GenericNameAr, cancellationToken);

        medicine.UpdateDetails(
            req.Name,
            req.Category,
            genericName,
            req.IsControlled,
            req.NameAr);

        if (req.IsActive) medicine.Activate();
        else medicine.Deactivate();

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
