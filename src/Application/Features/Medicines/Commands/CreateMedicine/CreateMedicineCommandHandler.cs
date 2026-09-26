using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Files.Common;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class CreateMedicineCommandHandler : IRequestHandler<CreateMedicineCommand, Result<Guid>>
{
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<GenericName> _generics;
    private readonly IUnitOfWork _uow;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateMedicineCommandHandler(IBaseRepository<Medicine> medicines, IBaseRepository<GenericName> generics, IUnitOfWork uow, IAttachmentUploadService attachments, IStringLocalizer<SharedResource> localizer)
    {
        _medicines = medicines;
        _generics = generics;
        _uow = uow;
        _attachments = attachments;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var nameSpec = new Specification<Medicine, Medicine>(m => m);
        nameSpec.Where(m => m.Name == req.Name.Trim());
        if (await _medicines.CountAsync(nameSpec, cancellationToken) > 0)
            return Result<Guid>.Failure(_localizer["MedicineAlreadyExists", req.Name].Value, 409);

        GenericName genericName = await MedicineMapping.ResolveGenericNameAsync(_generics, req.GenericName, req.GenericNameAr, cancellationToken);

        var medicine = req.ToEntity(req.Category, genericName);

        var seenKeys = new HashSet<(MedicineForm, MedicineUnit, decimal)>();

        foreach (var variantRequest in req.Variants)
        {
            var key = (variantRequest.Form, variantRequest.Unit, variantRequest.Strength);
            if (!seenKeys.Add(key))
                return Result<Guid>.Failure(
                    _localizer["VariantAlreadyExists", $"{variantRequest.Form} {variantRequest.Strength} {variantRequest.Unit}"].Value, 400);

            medicine.AddVariant(variantRequest.ToEntity(medicine.Id));
        }

        _medicines.Add(medicine);
        await _uow.SaveChangesAsync(cancellationToken);

        await _attachments.UploadAsync("Medicine", medicine.Id, req.File, cancellationToken);

        return Result<Guid>.Success(medicine.Id);
    }
}
