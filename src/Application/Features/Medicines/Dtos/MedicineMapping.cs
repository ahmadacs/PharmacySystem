using Application.Common.Interfaces;
using Application.Common.Specifications;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Features.Medicines.Dtos;

public static class MedicineMapping
{
    /// <summary>
    /// Finds the tracked scientific name or creates and tracks a new one, updating
    /// the Arabic name when it changed. Shared by the create/update medicine flows.
    /// </summary>
    public static async Task<GenericName> ResolveGenericNameAsync(
        IBaseRepository<GenericName> generics,
        string name,
        string? nameAr,
        CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        var trimmedAr = nameAr?.Trim();
        // Tracked: Rename below must persist on SaveChanges.
        var nameSpec = new Specification<GenericName, GenericName>(g => g).Tracked();
        nameSpec.Where(g => g.Name == trimmed);
        var genericName = await generics.GetAsync(nameSpec, cancellationToken);
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

    /// <summary>Maps a list-screen projection row (server-computed sums included).</summary>
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

    /// <summary>Maps a list-screen projection row (server-computed sums included).</summary>
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

   

    /// <summary>
    /// Assembles the details DTO from the single-query projection rows
    /// (see GetMedicineQueryHandler). DisplayName/IsLowStock rules are reused
    /// from the row mappings instead of being recomputed here.
    /// </summary>
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

    /// <summary>
    /// Maps a batches-list projection row (server-computed dispensed included).
    /// Same field semantics as the list screen: days-to-expiry is always the
    /// day difference (negative when expired), status is Expired/Depleted/Active.
    /// </summary>
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
            r.VariantName,
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