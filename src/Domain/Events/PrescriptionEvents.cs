namespace Domain.Events;

public abstract record DomainEvent(DateTime OccurredAtUtc);

public record PrescriptionCreatedEvent(Guid PrescriptionId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

public record PrescriptionCancelledEvent(Guid PrescriptionId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

public record PrescriptionRefilledEvent(Guid PrescriptionId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);

public record PrescriptionDispensedEvent(Guid PrescriptionId, DateTime OccurredAtUtc, int TotalDispensedQuantity)
    : DomainEvent(OccurredAtUtc);