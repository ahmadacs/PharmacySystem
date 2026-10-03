using Application.Common.Interfaces;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Features.Medicines.Dtos;

public static class MedicineMapping
{

    public static async Task<GenericName> ResolveGenericNameAsync(
        IRepository<GenericName> generics,
        string name,
        string? nameAr,
        CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        var trimmedAr = nameAr?.Trim();

        var genericName = await generics.GetAsync(g => g.Name == trimmed, tracked: true, cancellationToken: cancellationToken);
        if (genericName is not null)
        {
            if (!string.IsNullOrWhiteSpace(trimmedAr) && genericName.NameAr != trimmedAr)
                genericName.Rename(trimmed, trimmedAr);
            return genericName;
        }

        genericName = new GenericName(trimmed, trimmedAr);
        generics.Add(genericName);
        return genericName;
    }

    internal static MedicineVariantSummaryDto ToDto(this MedicineVariantRow v)
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

    internal static MedicineListItemDto ToDto(this MedicineRow r)
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
            variants.Count,
            variants.Any(v => v.IsLowStock));
    }

    internal static MedicineDetailsDto ToDto(this MedicineDetailsRow r, DateOnly asOf)
    {
        var variants = r.Variants.Select(v =>
        {
            var summary = v.Variant.ToDto();
            return new MedicineVariantDto(
                v.Variant.Id,
                r.Id,
                v.Variant.Form,
                v.Variant.Unit,
                v.Variant.Strength,
                summary.DisplayName,
                v.IsActive,
                v.Variant.AvailableQuantity,
                v.Variant.ReorderLevel,
                summary.IsLowStock,
                v.Variant.BaseUnitName,
                v.Variant.PackageUnitName,
                v.Variant.UnitsPerPackage,
                v.Variant.IsDivisible,
                v.Batches.Select(b => b.ToDto(asOf)).ToList());
        }).ToList();

        return new(
            r.Id,
            r.Name,
            r.NameAr,
            r.GenericName,
            r.GenericNameAr,
            r.Category,
            r.IsControlled,
            r.IsActive,
            variants.Sum(v => v.AvailableQuantity),
            variants);
    }

    internal static MedicineBatchDto ToDto(this MedicineBatchRow r, DateOnly asOf)
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
            $"{r.Form} {r.Strength} {r.Unit}",
            r.BatchNumber,
            r.ManufactureDate,
            r.ExpiryDate,
            r.QuantityReceived,
            r.QuantityAvailable,
            r.DispensedQuantity,
            r.UnitCost,
            r.SupplierName,
            isExpired,
            r.ExpiryDate.DayNumber - asOf.DayNumber,
            status,
            r.ReceivedDate);
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