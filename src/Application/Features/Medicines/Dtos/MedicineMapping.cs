using Domain.Common;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Features.Medicines.Dtos;

public static class MedicineMapping
{
    public static string ToDisplayValue(this MedicineForm form) => form.ToString();
    public static string ToDisplayValue(this MedicineUnit unit) => unit.ToString();
    public static string ToDisplayValue(this CategoryEnum category) => category.ToString();

    public static MedicineListItemDto ToListItemDto(this Medicine medicine, DateOnly asOf)
    {
        var available = medicine.GetAvailableStock(asOf).Value;
        var reorder = medicine.ReorderLevel.Value;
        var activeVariants = medicine.Variants.NotDeleted().Where(v => v.IsActive).ToList();

        return new MedicineListItemDto(
            medicine.Id,
            medicine.Name,
            medicine.NameAr,
            medicine.GenericName.Name,
            medicine.GenericName.NameAr,
            medicine.CategoryEnum,
            activeVariants.Select(v => new MedicineVariantSummaryDto(
                v.Id,
                v.Form,
                v.Unit,
                v.Strength,
                $"{v.Form} {v.Strength} {v.Unit}",
                v.GetAvailableStock(asOf).Value,
                v.UnitOfMeasure.BaseUnitName,
                v.UnitOfMeasure.PackageUnitName,
                v.UnitOfMeasure.UnitsPerPackage,
                v.UnitOfMeasure.IsDivisible)).ToList(),
            medicine.IsControlled,
            medicine.IsActive,
            reorder,
            available,
            activeVariants.Count,
            available <= reorder);
    }

    public static MedicineDetailsDto ToDetailsDto(this Medicine medicine, DateOnly asOf)
        => new(
            medicine.Id,
            medicine.Name,
            medicine.NameAr,
            medicine.GenericName.Name,
            medicine.GenericName.NameAr,
            medicine.CategoryEnum,
            medicine.IsControlled,
            medicine.IsActive,
            medicine.ReorderLevel.Value,
            medicine.GetAvailableStock(asOf).Value,
            medicine.Variants.NotDeleted()
                .OrderBy(v => v.Form)
                .ThenBy(v => v.Strength)
                .Select(v => v.ToDto(asOf))
                .ToList());

    public static MedicineVariantDto ToDto(this MedicineVariant variant, DateOnly asOf)
        => new(
            variant.Id,
            variant.MedicineId,
            variant.Form,
            variant.Unit,
            variant.Strength,
            $"{variant.Form} {variant.Strength} {variant.Unit}",
            variant.IsActive,
            variant.GetAvailableStock(asOf).Value,
            variant.UnitOfMeasure.BaseUnitName,
            variant.UnitOfMeasure.PackageUnitName,
            variant.UnitOfMeasure.UnitsPerPackage,
            variant.UnitOfMeasure.IsDivisible,
            variant.Batches.NotDeleted()
                .Select(b => b.ToDto(variant.Id, variant.Medicine?.Name ?? "Unknown", variant.Medicine?.NameAr, asOf))
                .OrderBy(b => b.ExpiryDate)
                .ToList());

    public static MedicineBatchDto ToDto(this MedicineBatch batch, Guid medicineVariantId, string medicineName, string? medicineNameAr, DateOnly asOf, int dispensedQuantity = 0)
    {
        int? daysToExpiry = null;
        if (!batch.IsExpired(asOf))
            daysToExpiry = batch.ExpiryDate.DayNumber - asOf.DayNumber;

        string batchStatus = batch.IsExpired(asOf)
            ? "Expired"
            : batch.QuantityAvailable.Value <= 0
                ? "Depleted"
                : "Active";

        // Variant display name from parts
        var variantName = batch.MedicineVariant is not null
            ? $"{batch.MedicineVariant.Form} {batch.MedicineVariant.Strength} {batch.MedicineVariant.Unit}"
            : "Unknown";

        return new MedicineBatchDto(
            batch.Id,
            medicineVariantId,
            medicineName,
            medicineNameAr,
            variantName,
            batch.BatchNumber,
            batch.ManufactureDate,
            batch.ExpiryDate,
            batch.QuantityReceived.Value,
            batch.QuantityAvailable.Value,
            dispensedQuantity,
            batch.UnitCost.Amount,
            batch.SupplierName,
            batch.IsExpired(asOf),
            daysToExpiry,
            batchStatus,
            batch.CreatedAt);
    }

    public static Medicine ToEntity(this CreateMedicineRequest request, CategoryEnum categoryEnum, GenericName genericName)
        => new(
            request.Name,
            categoryEnum,
            request.ReorderLevel,
            genericName,
            request.IsControlled,
            request.NameAr);

    public static MedicineVariant ToEntity(this MedicineVariantRequest request, Guid medicineId)
        => new(medicineId, request.Form, request.Unit, request.Strength,
            UnitOfMeasure.Create(request.BaseUnitName, request.PackageUnitName, request.UnitsPerPackage, request.IsDivisible));

    public static MedicineVariant ToEntity(this CreateVariantRequest request)
        => new(request.MedicineId, request.Form, request.Unit, request.Strength,
            UnitOfMeasure.Create(request.BaseUnitName, request.PackageUnitName, request.UnitsPerPackage, request.IsDivisible));

    public static MedicineBatch ToEntity(this AddBatchRequest request, UnitOfMeasure unitOfMeasure, string batchNumber)
        => new(
            request.MedicineVariantId,
            batchNumber,
            request.ManufactureDate,
            request.ExpiryDate,
            request.PackagesReceived,
            unitOfMeasure,
            request.UnitCost,
            request.SupplierName);
}