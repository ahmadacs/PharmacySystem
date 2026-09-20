using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Queries;

public sealed class GetMedicineQueryHandler : IRequestHandler<GetMedicineQuery, Result<MedicineDetailsDto>>
{
    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetMedicineQueryHandler(
        IMedicineRepository repo,
        IAsyncQueryExecutor executor,
        IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _executor = executor;
        _localizer = localizer;
    }

    public async Task<Result<MedicineDetailsDto>> Handle(GetMedicineQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var row = await _executor.SingleOrDefaultAsync(
            _repo.Query()
                .Where(m => m.Id == request.Id)
                .Select(m => new MedicineDetailsRow(
                    m.Id,
                    m.Name,
                    m.NameAr,
                    m.GenericName != null ? m.GenericName.Name : string.Empty,
                    m.GenericName != null ? m.GenericName.NameAr : null,
                    m.CategoryEnum,
                    m.IsControlled,
                    m.IsActive,
                    m.Variants
                        .OrderBy(v => v.Form)
                        .ThenBy(v => v.Strength)
                        .Select(v => new VariantWithBatchesRow(
                            v.IsActive,
                            new MedicineVariantRow(
                                v.Id,
                                v.Form,
                                v.Unit,
                                v.Strength,
                                v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                                v.ReorderLevel.Value,
                                v.UnitOfMeasure.BaseUnitName,
                                v.UnitOfMeasure.PackageUnitName,
                                v.UnitOfMeasure.UnitsPerPackage,
                                v.UnitOfMeasure.IsDivisible),
                            v.Batches
                                .OrderBy(b => b.ExpiryDate)
                                .Select(b => new MedicineBatchRow(
                                    b.Id,
                                    b.MedicineVariant!.MedicineId,
                                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.Name : "Unknown",
                                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.NameAr : null,
                                    $"{b.MedicineVariant!.Form} {b.MedicineVariant!.Strength} {b.MedicineVariant!.Unit}",
                                    b.BatchNumber,
                                    b.ManufactureDate,
                                    b.ExpiryDate,
                                    b.QuantityReceived.Value,
                                    b.QuantityAvailable.Value,
                                    b.UnitCost.Amount,
                                    b.SupplierName,
                                    b.CreatedAt,
                                    0))
                                .ToList()))
                        .ToList())),
            cancellationToken);

        if (row is null)
            return Result<MedicineDetailsDto>.Failure(_localizer["ResourceNotFound", "Medicine", request.Id].Value, 404);

        return Result<MedicineDetailsDto>.Success(row.ToDto(asOf));
    }
}