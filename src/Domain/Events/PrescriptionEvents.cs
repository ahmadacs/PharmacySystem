namespace Domain.Events;

using Domain.Common;

public abstract record DomainEvent(DateTime OccurredAtUtc) : IDomainEvent;

public record PrescriptionCreatedEvent(Guid PrescriptionId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

public record PrescriptionCancelledEvent(Guid PrescriptionId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

public record PrescriptionRefilledEvent(
    Guid PrescriptionId,
    IReadOnlyList<Guid> PrescriptionItemIds,
    DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

public record PrescriptionDispensedEvent(Guid PrescriptionId, DateTime OccurredAtUtc, int TotalDispensedQuantity)
    : DomainEvent(OccurredAtUtc);