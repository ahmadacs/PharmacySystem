using Domain.Common;
using Domain.Entities.Medicines;
using Domain.Enums;

namespace Domain.Entities.Inventory;

/// <summary>
/// A single stock movement against exactly one medicine batch. The adjustment sign
/// is derived from the type: increasing types (Increase, Returned, TransferIn) are
/// positive, every other type is negative.
/// <para>
/// UNIFIED before/after rule: <see cref="QuantityBefore"/> and <see cref="QuantityAfter"/>
/// always represent the quantity of the referenced batch (<see cref="MedicineBatchId"/>)
/// immediately before and after the movement. For a brand-new batch receive the batch is
/// created at that moment, so before = 0 and after = the received units — the same meaning
/// as for an existing batch, never a medicine/variant-level total.
/// </para>
/// </summary>
public class InventoryAdjustment : BaseEntity
{
    public Guid MedicineBatchId { get; private set; }
    public MedicineBatch? MedicineBatch { get; private set; }

    public InventoryAdjustmentType Type { get; private set; }
    public int QuantityChanged { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// The user who performed the adjustment. Mandatory for the types that reduce
    /// stock (Decrease, Sold, Damaged, Expired, TransferOut, Correction); optional
    /// for the increasing types (Increase, Returned, TransferIn).
    /// </summary>
    public Guid? AdjustedBy { get; private set; }

    /// <summary>
    /// When the adjustment happened. ALWAYS stored in UTC: a caller-provided value
    /// is normalized to UTC (a Local value is converted, an Unspecified value is
    /// assumed to be UTC), and when omitted the current UTC time is used. This is
    /// the single source of truth for the adjustment timestamp; the API layer only
    /// converts to the display timezone (Asia/Riyadh) for presentation.
    /// </summary>
    public DateTime AdjustedAt { get; private set; }

    private InventoryAdjustment() { }

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

        var isIncreaseType = type is InventoryAdjustmentType.Increase or InventoryAdjustmentType.Returned
            or InventoryAdjustmentType.TransferIn;
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

    /// <summary>
    /// Normalizes a DateTime to UTC so the stored value is always UTC regardless
    /// of how the caller produced it (see the <see cref="AdjustedAt"/> remarks).
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}