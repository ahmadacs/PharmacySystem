using Domain.Common;
using Domain.Entities.Medicines;
using Domain.Enums;

namespace Domain.Entities.Inventory;

public class InventoryAdjustment : BaseEntity
{
    public Guid MedicineBatchId { get; private set; }
    public MedicineBatch? MedicineBatch { get; private set; }

    public InventoryAdjustmentType Type { get; private set; }
    public int QuantityChanged { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public Guid? AdjustedBy { get; private set; }

    public DateTime AdjustedAt { get; private set; }

    private InventoryAdjustment() { }

    public static bool IsIncreaseType(InventoryAdjustmentType type)
        => type is InventoryAdjustmentType.Increase or InventoryAdjustmentType.Returned
            or InventoryAdjustmentType.TransferIn;

    public static int SignedQuantity(InventoryAdjustmentType type, int quantity)
        => IsIncreaseType(type) ? quantity : -quantity;

    public InventoryAdjustment(Guid medicineBatchId, InventoryAdjustmentType type, int quantityChanged,
        string reason, Guid? adjustedBy, int quantityBefore, int quantityAfter, DateTime? adjustedAt = null)
    {
        if (medicineBatchId == Guid.Empty)
            throw new ArgumentException("MedicineBatchId is required.", nameof(medicineBatchId));
        if (quantityChanged == 0)
            throw new ArgumentException("Quantity changed cannot be zero.", nameof(quantityChanged));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required for every inventory adjustment.", nameof(reason));
        if (quantityBefore < 0)
            throw new ArgumentException("Quantity before the adjustment cannot be negative.", nameof(quantityBefore));
        if (quantityAfter != quantityBefore + quantityChanged)
            throw new ArgumentException("Quantity after must equal quantity before plus the change.", nameof(quantityAfter));

        var isIncreaseType = IsIncreaseType(type);
        if (isIncreaseType && quantityChanged < 0)
            throw new ArgumentException("Quantity must be positive for an increasing adjustment type.", nameof(quantityChanged));
        if (!isIncreaseType && quantityChanged > 0)
            throw new ArgumentException("Quantity must be negative for a decreasing adjustment type.", nameof(quantityChanged));
        if (!isIncreaseType && !adjustedBy.HasValue)
            throw new ArgumentException("AdjustedBy is required when the adjustment reduces stock.", nameof(adjustedBy));

        MedicineBatchId = medicineBatchId;
        Type = type;
        QuantityChanged = quantityChanged;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        Reason = reason.Trim();
        AdjustedBy = adjustedBy;
        AdjustedAt = ToUtc(adjustedAt ?? DateTime.UtcNow);
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}