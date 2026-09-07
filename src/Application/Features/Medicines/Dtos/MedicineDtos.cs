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
    int ReorderLevel,
    bool IsLowStock,
    string BaseUnitName,
    string PackageUnitName,
    int UnitsPerPackage,
    bool IsDivisible);

/// <summary>
/// EF projection row for one variant in the medicines list.
/// Never constructed outside queries; maps via <c>MedicineMapping.ToDto</c>.
/// </summary>
public sealed record MedicineVariantRow(
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

/// <summary>
/// EF projection row for one medicine in the medicines list.
/// Never constructed outside queries; maps via <c>MedicineMapping.ToDto</c>.
/// </summary>
public sealed record MedicineRow(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    CategoryEnum Category,
    bool IsControlled,
    bool IsActive,
    IReadOnlyList<MedicineVariantRow> Variants,
    int VariantCount);

/// <summary>
/// EF projection row for one batch in the batches list.
/// Never constructed outside queries; maps via <c>MedicineMapping.ToDto</c>.
/// Note: <c>MedicineId</c> is the parent medicine id (as the list screen shows).
/// </summary>
public sealed record MedicineBatchRow(
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
    decimal UnitCostAmount,
    string? SupplierName,
    DateTime CreatedAt,
    int DispensedQuantity);

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