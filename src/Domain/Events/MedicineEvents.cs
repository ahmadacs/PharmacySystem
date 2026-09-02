namespace Domain.Events;

/// <summary>
/// Raised when a medicine variant's available stock falls at or below its reorder level.
/// Emitted from stock-changing operations (dispensing, inventory adjustments).
/// </summary>
public record MedicineLowStockEvent(
    Guid MedicineId,
    Guid MedicineVariantId,
    string MedicineName,
    string VariantName,
    int AvailableStock,
    int ReorderLevel,
    DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

/// <summary>
/// Raised when a batch's expiry date is at most the near-expiry window away and
/// the batch is not yet expired. Emitted from stock-changing operations.
/// </summary>
public record MedicineBatchNearExpiryEvent(
    Guid MedicineBatchId,
    Guid MedicineVariantId,
    string BatchNumber,
    DateOnly ExpiryDate,
    DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);