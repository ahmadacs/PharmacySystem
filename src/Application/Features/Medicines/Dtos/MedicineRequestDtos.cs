using System.ComponentModel.DataAnnotations;
using Application.Common.Attributes;
using Domain.Enums;

namespace Application.Features.Medicines.Dtos;

public sealed record MedicineVariantRequest
{
    [EnumDataType(typeof(MedicineForm))]
    public MedicineForm Form { get; init; }

    [EnumDataType(typeof(MedicineUnit))]
    public MedicineUnit Unit { get; init; }

    [Range(0.01, (double)decimal.MaxValue)]
    public decimal Strength { get; init; }

    [Required, StringLength(50)]
    public string BaseUnitName { get; init; } = string.Empty;

    [Required, StringLength(50)]
    public string PackageUnitName { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int UnitsPerPackage { get; init; } = 1;

    public bool IsDivisible { get; init; } = true;
}

public sealed record CreateMedicineRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(200)]
    public string? NameAr { get; init; }

    [StringLength(200)]
    public string? GenericNameAr { get; init; }

    [Required, StringLength(200)]
    public string GenericName { get; init; } = string.Empty;

    [EnumDataType(typeof(CategoryEnum))]
    public CategoryEnum Category { get; init; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; init; }

    public bool IsControlled { get; init; }

    [MinLength(1)]
    public List<MedicineVariantRequest> Variants { get; init; } = [];
}

public sealed record UpdateMedicineRequest
{
    public Guid Id { get; init; }

    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(200)]
    public string? NameAr { get; init; }

    [StringLength(200)]
    public string? GenericNameAr { get; init; }

    [Required, StringLength(200)]
    public string GenericName { get; init; } = string.Empty;

    [EnumDataType(typeof(CategoryEnum))]
    public CategoryEnum Category { get; init; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; init; }

    public bool IsControlled { get; init; }
    public bool IsActive { get; init; }
}

public sealed record AddBatchRequest
{
    public Guid MedicineVariantId { get; init; }
    // BatchNumber removed; generated server-side.
    [NotInTheFuture]
    public DateOnly ManufactureDate { get; init; }

    [NotInThePast]
    public DateOnly ExpiryDate { get; init; }

    [PositiveQuantity]
    [Range(1, int.MaxValue)]
    public int PackagesReceived { get; init; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal UnitCost { get; init; }

    [StringLength(200)]
    public string? SupplierName { get; init; }

    [EnumDataType(typeof(Domain.Enums.InventoryAdjustmentType))]
    public Domain.Enums.InventoryAdjustmentType AdjustmentType { get; init; } = Domain.Enums.InventoryAdjustmentType.Increase;
}

public sealed record CreateVariantRequest
{
    public Guid MedicineId { get; init; }

    [EnumDataType(typeof(MedicineForm))]
    public MedicineForm Form { get; init; }

    [EnumDataType(typeof(MedicineUnit))]
    public MedicineUnit Unit { get; init; }

    [Range(0.01, (double)decimal.MaxValue)]
    public decimal Strength { get; init; }

    [Required, StringLength(50)]
    public string BaseUnitName { get; init; } = string.Empty;

    [Required, StringLength(50)]
    public string PackageUnitName { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int UnitsPerPackage { get; init; } = 1;

    public bool IsDivisible { get; init; } = true;
}