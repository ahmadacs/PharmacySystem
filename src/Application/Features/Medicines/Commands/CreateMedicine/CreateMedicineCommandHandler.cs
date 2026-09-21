using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class CreateMedicineCommandHandler : IRequestHandler<CreateMedicineCommand, Result<Guid>>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateMedicineCommandHandler(IMedicineRepository repo, IUnitOfWork uow, IAttachmentUploadService attachments, IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _uow = uow;
        _attachments = attachments;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _repo.MedicineNameExistsAsync(req.Name, null, cancellationToken))
            return Result<Guid>.Failure(_localizer["MedicineAlreadyExists", req.Name].Value, 409);

        GenericName genericName = await MedicineMapping.ResolveGenericNameAsync(_repo, req.GenericName, req.GenericNameAr, cancellationToken);

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

        _repo.Add(medicine);
        await _uow.SaveChangesAsync(cancellationToken);

        await _attachments.UploadAsync("Medicine", medicine.Id, req.File, cancellationToken);

        return Result<Guid>.Success(medicine.Id);
    }
}