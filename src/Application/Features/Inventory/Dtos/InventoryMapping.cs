using Application.Features.Medicines.Dtos;
using Domain.Entities.Inventory;
using Domain.Enums;

namespace Application.Features.Inventory.Dtos;

public static class InventoryMapping
{

    public static AddBatchRequest ToAddBatchRequest(this ReceiveInventoryRequest request)
        => new()
        {
            MedicineVariantId = request.MedicineVariantId,

            ManufactureDate = request.ManufactureDate,
            ExpiryDate = request.ExpiryDate,
            PackagesReceived = request.PackagesReceived,
            UnitCost = request.UnitCost,
            SupplierName = request.SupplierName,
            AdjustmentType = request.AdjustmentType
        };

    private const int CriticalWithinDays = 30;
    private const int WarningWithinDays = 90;

    internal static LowStockDto ToDto(this LowStockRow r)
        => new(
            r.MedicineId,
            r.MedicineName,
            r.MedicineNameAr,
            r.MedicineVariantId,
            $"{r.Form} {r.Strength} {r.Unit}",
            r.AvailableQuantity,
            r.ReorderLevel,
            r.Form,
            r.Unit,
            r.Strength);

    internal static MedicineInventorySummaryDto ToDto(this MedicineInventorySummaryRow r)
        => new(
            r.Id,
            r.Name,
            r.NameAr,
            r.GenericName,
            r.GenericNameAr,
            r.VariantCount,
            r.TotalQuantity,
            r.ReorderLevel,
            GetStockStatus(r.TotalQuantity, r.HasLowVariant),
            r.NearestExpiryDate,
            r.ActiveBatchCount);

    internal static ExpiryAlertDto ToDto(this ExpiryAlertRow r, DateOnly asOf)
    {
        var daysToExpiry = r.ExpiryDate.DayNumber - asOf.DayNumber;
        return new(
            r.BatchId,
            r.MedicineName,
            r.MedicineNameAr,
            $"{r.Form} {r.Strength} {r.Unit}",
            r.BatchNumber,
            r.ExpiryDate,
            daysToExpiry,
            r.RemainingQuantity,
            GetExpiryStatus(daysToExpiry));
    }

    internal static InventoryAdjustmentDto ToDto(this InventoryAdjustmentRow r, string? adjustedByName)
        => new(
            r.Id,
            r.MedicineBatchId,
            r.MedicineName,
            r.MedicineNameAr,
            r.Strength is null ? $"{r.Form} {r.Unit}".Trim() : $"{r.Form} {r.Strength} {r.Unit}",
            r.BatchNumber,
            r.Type.ToString(),
            r.QuantityChanged,
            r.QuantityBefore,
            r.QuantityAfter,
            r.Reason,
            r.AdjustedBy,
            adjustedByName,
            r.AdjustedAt);

    private static string GetStockStatus(int totalQuantity, bool hasLowVariant)
        => totalQuantity == 0
            ? InventoryStatus.OutOfStock
            : hasLowVariant
                ? InventoryStatus.LowStock
                : InventoryStatus.InStock;

    private static string GetExpiryStatus(int daysToExpiry)
        => daysToExpiry < 0
            ? ExpiryStatus.Expired
            : daysToExpiry < CriticalWithinDays
                ? ExpiryStatus.Critical
                : daysToExpiry < WarningWithinDays
                    ? ExpiryStatus.Warning
                    : ExpiryStatus.Safe;

    public static InventoryAdjustment ToEntity(this AdjustInventoryRequest request, Guid medicineBatchId,
        Guid? adjustedBy, int quantityBefore, int quantityAfter)
    {
        var isIncrease = request.Type is InventoryAdjustmentType.Increase or InventoryAdjustmentType.Returned
            or InventoryAdjustmentType.TransferIn;
        var delta = isIncrease ? request.Quantity : -request.Quantity;

        return new InventoryAdjustment(
            medicineBatchId,
            request.Type,
            delta,
            request.Reason,
            adjustedBy,
            quantityBefore,
            quantityAfter,
            DateTime.UtcNow);
    }

    public static InventoryAdjustment ToEntity(
        this AddBatchRequest request,
        Guid batchId,
        int quantityChanged,
        Guid? adjustedBy,
        int quantityBefore,
        int quantityAfter,
        string reason)
        => new(
            batchId,
            request.AdjustmentType,
            quantityChanged,
            reason,
            adjustedBy,
            quantityBefore,
            quantityAfter,
            DateTime.UtcNow);
}