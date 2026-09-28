namespace Application.Features.Inventory.Dtos;

internal sealed record LowStockRow(
    Guid MedicineId,
    string MedicineName,
    string? MedicineNameAr,
    Guid MedicineVariantId,
    int AvailableQuantity,
    int ReorderLevel,
    Domain.Enums.MedicineForm Form,
    Domain.Enums.MedicineUnit Unit,
    decimal Strength);

internal sealed record MedicineInventorySummaryRow(
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

internal sealed record ExpiryAlertRow(
    Guid BatchId,
    string MedicineName,
    string? MedicineNameAr,
    Domain.Enums.MedicineForm Form,
    Domain.Enums.MedicineUnit Unit,
    decimal Strength,
    string BatchNumber,
    DateOnly ExpiryDate,
    int RemainingQuantity);

internal sealed record InventoryAdjustmentRow(
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