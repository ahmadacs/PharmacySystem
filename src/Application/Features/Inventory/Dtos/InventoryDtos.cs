namespace Application.Features.Inventory.Dtos;

/// <summary>
/// EF projection row for the low-stock list.
/// Never constructed outside queries; maps via <c>InventoryMapping.ToDto</c>.
/// </summary>
public sealed record LowStockRow(
    Guid MedicineId,
    string MedicineName,
    string? MedicineNameAr,
    Guid MedicineVariantId,
    int AvailableQuantity,
    int ReorderLevel,
    Domain.Enums.MedicineForm Form,
    Domain.Enums.MedicineUnit Unit,
    decimal Strength);

/// <summary>
/// EF projection row for the inventory summary list.
/// Never constructed outside queries; maps via <c>InventoryMapping.ToDto</c>.
/// </summary>
public sealed record MedicineInventorySummaryRow(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    int VariantCount,
    int TotalQuantity,
    int ReorderLevel,
    bool HasLowVariant,
    DateOnly? NearestExpiryDate,
    int ActiveBatchCount);

/// <summary>
/// EF projection row for the expiry-alerts list.
/// Never constructed outside queries; maps via <c>InventoryMapping.ToDto</c>.
/// </summary>
public sealed record ExpiryAlertRow(
    Guid BatchId,
    string MedicineName,
    string? MedicineNameAr,
    Domain.Enums.MedicineForm? Form,
    Domain.Enums.MedicineUnit? Unit,
    decimal? Strength,
    string BatchNumber,
    DateOnly ExpiryDate,
    int DaysToExpiry,
    int RemainingQuantity);

/// <summary>
/// EF projection row for the adjustments list.
/// Never constructed outside queries; maps via <c>InventoryMapping.ToDto</c>.
/// </summary>
public sealed record InventoryAdjustmentRow(
    Guid Id,
    Guid MedicineBatchId,
    string MedicineName,
    string? MedicineNameAr,
    Domain.Enums.MedicineForm? Form,
    Domain.Enums.MedicineUnit? Unit,
    decimal? Strength,
    string BatchNumber,
    Domain.Enums.InventoryAdjustmentType Type,
    int QuantityChanged,
    int QuantityBefore,
    int QuantityAfter,
    string Reason,
    Guid? AdjustedBy,
    DateTime AdjustedAt);

public sealed record LowStockDto(
    Guid MedicineId,
    string MedicineName,
    string? MedicineNameAr,
    Guid MedicineVariantId,
    string VariantName,
    int AvailableQuantity,
    int ReorderLevel,
    Domain.Enums.MedicineForm Form,
    Domain.Enums.MedicineUnit Unit,
    decimal Strength);

/// <summary>
/// Aggregated per-medicine inventory row (sums across all its variants and their
/// batches). All quantity fields are computed from the active batches at query time.
/// </summary>
public sealed record MedicineInventorySummaryDto(
    Guid Id,
    string Name,
    string? NameAr,
    string GenericName,
    string? GenericNameAr,
    int VariantCount,
    int TotalQuantity,
    int ReorderLevel,
    string StockStatus,
    DateOnly? NearestExpiryDate,
    int ActiveBatchCount);

/// <summary>
/// A single batch flagged for expiry attention. Status is computed internally
/// with UTC "today": Critical when the batch expires within 30 days, Warning
/// within 90 days, Safe otherwise, and Expired when past its expiry date.
/// </summary>
public sealed record ExpiryAlertDto(
    Guid BatchId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    string BatchNumber,
    DateOnly ExpiryDate,
    int DaysRemaining,
    int RemainingQuantity,
    string Status);

public sealed record InventoryAdjustmentDto(
    Guid Id,
    Guid MedicineBatchId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    string BatchNumber,
    string Type,
    int QuantityChanged,
    int QuantityBefore,
    int QuantityAfter,
    string Reason,
    Guid? AdjustedBy,
    string? AdjustedByName,
    DateTime AdjustedAt);

public static class InventoryStatus
{
    public const string InStock = "InStock";
    public const string LowStock = "LowStock";
    public const string OutOfStock = "OutOfStock";
}

public static class ExpiryStatus
{
    public const string Expired = "Expired";
    public const string Critical = "Critical";
    public const string Warning = "Warning";
    public const string Safe = "Safe";
}