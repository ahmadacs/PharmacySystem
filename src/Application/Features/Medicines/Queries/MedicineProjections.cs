using System.Linq.Expressions;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;

namespace Application.Features.Medicines.Queries;

internal static class MedicineProjections
{
    public static Expression<Func<Medicine, MedicineDetailsRow>> ToDetailsRow(DateOnly asOf) => m => new MedicineDetailsRow(
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
                        m.Id,
                        m.Name,
                        m.NameAr,
                        v.Form,
                        v.Unit,
                        v.Strength,
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
            .ToList());

    public static Expression<Func<Medicine, MedicineRow>> ToListRow(DateOnly asOf) => m => new MedicineRow(
        m.Id,
        m.Name,
        m.NameAr,
        m.GenericName != null ? m.GenericName.Name : string.Empty,
        m.GenericName != null ? m.GenericName.NameAr : null,
        m.CategoryEnum,
        m.IsControlled,
        m.IsActive,
        m.Variants
            .Where(v => v.IsActive)
            .OrderBy(v => v.Form)
            .Select(v => new MedicineVariantRow(
                v.Id,
                v.Form,
                v.Unit,
                v.Strength,
                v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                v.ReorderLevel.Value,
                v.UnitOfMeasure.BaseUnitName,
                v.UnitOfMeasure.PackageUnitName,
                v.UnitOfMeasure.UnitsPerPackage,
                v.UnitOfMeasure.IsDivisible))
            .ToList(),
        m.CreatedAt,
        m.Variants.Where(v => v.IsActive).OrderBy(v => v.Form).Select(v => (Domain.Enums.MedicineForm?)v.Form).FirstOrDefault());
}
