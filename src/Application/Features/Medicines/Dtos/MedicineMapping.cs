using Application.Common.Interfaces;
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

    public static CategoryDto ToDto(this CategoryEnum category)
        => new((int)category, category.ToDisplayValue(), null);

    /// <summary>
    /// Finds the tracked scientific name or creates and tracks a new one, updating
    /// the Arabic name when it changed. Shared by the create/update medicine flows.
    /// </summary>
    public static async Task<GenericName> ResolveGenericNameAsync(
        IMedicineRepository repo,
        string name,
        string? nameAr,
        CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        var trimmedAr = nameAr?.Trim();
        var genericName = await repo.FindGenericNameAsync(trimmed, cancellationToken);
        if (genericName is not null)
        {
            if (!string.IsNullOrWhiteSpace(trimmedAr) && genericName.NameAr != trimmedAr)
                genericName.Rename(trimmed, trimmedAr);
            return genericName;
        }

        genericName = new GenericName(trimmed, trimmedAr);
        repo.AddGenericName(genericName);
        return genericName;
    }

    /// <summary>Maps a list-screen projection row (server-computed sums included).</summary>
    public static MedicineVariantSummaryDto ToDto(this MedicineVariantRow v)
        => new(
            v.Id,
            v.Form,
            v.Unit,
            v.Strength,
            $"{v.Form} {v.Strength} {v.Unit}",
            v.AvailableQuantity,
            v.ReorderLevel,
            v.AvailableQuantity <= v.ReorderLevel,
            v.BaseUnitName,
            v.PackageUnitName,
            v.UnitsPerPackage,
            v.IsDivisible);

    /// <summary>Maps a list-screen projection row (server-computed sums included).</summary>
    public static MedicineListItemDto ToDto(this MedicineRow r)
    {
        var variants = r.Variants.Select(v => v.ToDto()).ToList();

        return new(
            r.Id,
            r.Name,
            r.NameAr,
            r.GenericName,
            r.GenericNameAr,
            r.Category,
            variants,
            r.IsControlled,
            r.IsActive,
            variants.Sum(v => v.AvailableQuantity),
            r.VariantCount,
            variants.Any(v => v.IsLowStock));
    }

    public static MedicineListItemDto ToListItemDto(this Medicine medicine, DateOnly asOf)
    {
        var activeVariants = medicine.Variants.NotDeleted().Where(v => v.IsActive).ToList();
        var row = new MedicineRow(
            medicine.Id,
            medicine.Name,
            medicine.NameAr,
            medicine.GenericName.Name,
            medicine.GenericName.NameAr,
            medicine.CategoryEnum,
            medicine.IsControlled,
            medicine.IsActive,
            activeVariants.Select(v => new MedicineVariantRow(
                v.Id,
                v.Form,
                v.Unit,
                v.Strength,
                v.GetAvailableStock(asOf).Value,
                v.ReorderLevel.Value,
                v.UnitOfMeasure.BaseUnitName,
                v.UnitOfMeasure.PackageUnitName,
                v.UnitOfMeasure.UnitsPerPackage,
                v.UnitOfMeasure.IsDivisible)).ToList(),
            activeVariants.Count);

        return row.ToDto();
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
            variant.ReorderLevel.Value,
            variant.IsLowStock(asOf),
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

    /// <summary>
    /// Maps a batches-list projection row (server-computed dispensed included).
    /// Same field semantics as the list screen: days-to-expiry is always the
    /// day difference (negative when expired), status is Expired/Depleted/Active.
    /// </summary>
    public static MedicineBatchDto ToDto(this MedicineBatchRow r, DateOnly asOf)
    {
        var isExpired = r.ExpiryDate <= asOf;
        var status = isExpired
            ? "Expired"
            : r.QuantityAvailable <= 0
                ? "Depleted"
                : "Active";

        return new(
            r.Id,
            r.MedicineId,
            r.MedicineName,
            r.MedicineNameAr,
            r.VariantName,
            r.BatchNumber,
            r.ManufactureDate,
            r.ExpiryDate,
            r.QuantityReceived,
            r.QuantityAvailable,
            r.DispensedQuantity,
            r.UnitCostAmount,
            r.SupplierName,
            isExpired,
            r.ExpiryDate.DayNumber - asOf.DayNumber,
            status,
            r.CreatedAt);
    }

    public static Medicine ToEntity(this CreateMedicineRequest request, CategoryEnum categoryEnum, GenericName genericName)
        => new(
            request.Name,
            categoryEnum,
            genericName,
            request.IsControlled,
            request.NameAr);

    public static MedicineVariant ToEntity(this MedicineVariantRequest request, Guid medicineId)
        => new(medicineId, request.Form, request.Unit, request.Strength, request.ReorderLevel,
            UnitOfMeasure.Create(request.BaseUnitName, request.PackageUnitName, request.UnitsPerPackage, request.IsDivisible));

    public static MedicineVariant ToEntity(this CreateVariantRequest request)
        => new(request.MedicineId, request.Form, request.Unit, request.Strength, request.ReorderLevel,
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