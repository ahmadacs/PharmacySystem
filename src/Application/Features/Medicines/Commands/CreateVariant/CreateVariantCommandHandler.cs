using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class CreateVariantCommandHandler : IRequestHandler<CreateVariantCommand, Result<Guid>>
{
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<MedicineVariant> _variants;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateVariantCommandHandler(IBaseRepository<Medicine> medicines, IBaseRepository<MedicineVariant> variants, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _medicines = medicines;
        _variants = variants;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateVariantCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var byIdSpec = new Specification<Medicine, Medicine>(m => m).Tracked();
        byIdSpec.Where(m => m.Id == req.MedicineId);
        var medicine = await _medicines.GetAsync(byIdSpec, cancellationToken);
        if (medicine is null)
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "Medicine", req.MedicineId].Value, 404);

        var existingSpec = new Specification<MedicineVariant, MedicineVariant>(v => v);
        existingSpec.Where(v => v.MedicineId == req.MedicineId && v.Form == req.Form && v.Unit == req.Unit && v.Strength == req.Strength);
        var existing = await _variants.GetAsync(existingSpec, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure(
                _localizer["VariantAlreadyExists", $"{req.Form} {req.Strength} {req.Unit}"].Value, 409);

        var variant = req.ToEntity();
        _variants.Add(variant);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(variant.Id);
    }
}
