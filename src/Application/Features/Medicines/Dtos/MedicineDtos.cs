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

/// <summary>
/// Internal EF projection shape (SQL-translatable raw fields only).
/// Never serialized to the API; maps via <c>MedicineMapping.ToDto</c>
/// where display names and statuses are computed in memory.
/// </summary>
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

/// <summary>
/// Internal EF projection shape for the medicines list.
/// No computed fields here: counts and stock flags are derived in
/// <c>MedicineMapping.ToDto</c> from <c>Variants</c> (single source of truth).
/// </summary>
internal sealed record MedicineRow(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    CategoryEnum Category,
    bool IsControlled,
    bool IsActive,
    IReadOnlyList<MedicineVariantRow> Variants);

/// <summary>
/// Internal EF projection shape for one batch.
/// Field names mirror <see cref="MedicineBatchDto"/> 1:1; status/expiry math
/// lives in <c>MedicineMapping.ToDto</c> (not translatable to SQL).
/// Note: <c>MedicineId</c> is the parent medicine id (as the list screen shows).
/// </summary>
internal sealed record MedicineBatchRow(
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
    decimal UnitCost,
    string? SupplierName,
    DateTime ReceivedDate,
    int DispensedQuantity);

/// <summary>
/// Internal single-query shape for the details screen: header +
/// variant rows, each carrying its own batch rows.
/// </summary>
internal sealed record VariantWithBatchesRow(
    bool IsActive,
    MedicineVariantRow Variant,
    IReadOnlyList<MedicineBatchRow> Batches);

/// <summary>
/// Internal single-query shape for the medicine details screen.
/// </summary>
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