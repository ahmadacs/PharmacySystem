using Application.Common.Interfaces;
using Application.Common.Models;
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

        if (!await _medicines.ExistsAsync(m => m.Id == req.MedicineId, cancellationToken))
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "Medicine", req.MedicineId].Value, 404);
        if (await _variants.ExistsAsync(
                v => v.MedicineId == req.MedicineId && v.Form == req.Form && v.Unit == req.Unit && v.Strength == req.Strength,
                cancellationToken))
            return Result<Guid>.Failure(
                _localizer["VariantAlreadyExists", $"{req.Form} {req.Strength} {req.Unit}"].Value, 409);

        var variant = req.ToEntity();
        _variants.Add(variant);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(variant.Id);
    }
}
