using Domain.Enums;

namespace Application.Features.Medicines.Dtos;

public sealed record CategoryDto(
    int Id,
    string Name,
    string? NameAr);

public sealed record MedicineVariantSummaryDto(
    Guid Id,
    MedicineForm Form,
    MedicineUnit Unit,
    decimal Strength,
    string DisplayName,
    int AvailableQuantity,
    string BaseUnitName,
    string PackageUnitName,
    int UnitsPerPackage,
    bool IsDivisible);

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
    int ReorderLevel,
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
    int ReorderLevel,
    int AvailableQuantity,
    IReadOnlyList<MedicineVariantDto> Variants);

public sealed record MedicineBatchDto(
    Guid Id,
    Guid MedicineVariantId,
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