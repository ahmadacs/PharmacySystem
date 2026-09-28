using Domain.Enums;

namespace Application.Features.Medicines.Dtos;

public sealed record MedicineVariantSummaryDto(
    Guid Id,
    MedicineForm Form,
    MedicineUnit Unit,
    decimal Strength,
    string DisplayName,
    int AvailableQuantity,
    int ReorderLevel,
    bool IsLowStock,
    string BaseUnitName,
    string PackageUnitName,
    int UnitsPerPackage,
    bool IsDivisible);

internal sealed record MedicineVariantRow(
    Guid Id,
    MedicineForm Form,
    MedicineUnit Unit,
    decimal Strength,
    int AvailableQuantity,
    int ReorderLevel,
    string BaseUnitName,
    string PackageUnitName,
    int UnitsPerPackage,
    bool IsDivisible);

internal sealed record MedicineRow(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    CategoryEnum Category,
    bool IsControlled,
    bool IsActive,
    IReadOnlyList<MedicineVariantRow> Variants,
    DateTime CreatedAt,
    Domain.Enums.MedicineForm? FirstVariantForm);

internal sealed record MedicineBatchRow(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    string? MedicineNameAr,
    MedicineForm Form,
    MedicineUnit Unit,
    decimal Strength,
    string BatchNumber,
    DateOnly ManufactureDate,
    DateOnly ExpiryDate,
    int QuantityReceived,
    int QuantityAvailable,
    decimal UnitCost,
    string? SupplierName,
    DateTime ReceivedDate,
    int DispensedQuantity);

internal sealed record VariantWithBatchesRow(
    bool IsActive,
    MedicineVariantRow Variant,
    IReadOnlyList<MedicineBatchRow> Batches);

internal sealed record MedicineDetailsRow(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    CategoryEnum Category,
    bool IsControlled,
    bool IsActive,
    IReadOnlyList<VariantWithBatchesRow> Variants);

public sealed record MedicineListItemDto(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    CategoryEnum Category,
    IReadOnlyList<MedicineVariantSummaryDto> Variants,
    bool IsControlled,
    bool IsActive,
    int AvailableQuantity,
    int VariantCount,
    bool IsLowStock);

public sealed record MedicineVariantDto(
    Guid Id,
    Guid MedicineId,
    MedicineForm Form,
    MedicineUnit Unit,
    decimal Strength,
    string DisplayName,
    bool IsActive,
    int AvailableQuantity,
    int ReorderLevel,
    bool IsLowStock,
    string BaseUnitName,
    string PackageUnitName,
    int UnitsPerPackage,
    bool IsDivisible,
    IReadOnlyList<MedicineBatchDto> Batches);

public sealed record MedicineDetailsDto(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    CategoryEnum Category,
    bool IsControlled,
    bool IsActive,
    int AvailableQuantity,
    IReadOnlyList<MedicineVariantDto> Variants);

public sealed record MedicineBatchDto(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    string BatchNumber,
    DateOnly ManufactureDate,
    DateOnly ExpiryDate,
    int QuantityReceived,
    int QuantityAvailable,
    int DispensedQuantity,
    decimal UnitCost,
    string? SupplierName,
    bool IsExpired,
    int? DaysToExpiry,
    string BatchStatus,
    DateTime ReceivedDate);