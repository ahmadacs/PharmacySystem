namespace Domain.Events;

public record MedicineLowStockEvent(
    Guid MedicineId,
    Guid MedicineVariantId,
    string MedicineName,
    string VariantName,
    int AvailableStock,
    int ReorderLevel,
    DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

public record MedicineBatchNearExpiryEvent(
    Guid MedicineBatchId,
    Guid MedicineVariantId,
    string BatchNumber,
    DateOnly ExpiryDate,
    DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);