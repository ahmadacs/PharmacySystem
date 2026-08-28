using Application.Common.Interfaces;
using Application.Features.Notifications.Events;
using Application.Features.Prescriptions.Events;
using Domain.Events;
using MediatR;

namespace Infrastructure.Services;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAsync(IReadOnlyCollection<object> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notification = ToNotification(domainEvent);
            await _mediator.Publish(notification, cancellationToken);
        }
    }

    private static INotification ToNotification(object domainEvent) => domainEvent switch
    {
        PrescriptionCreatedEvent e => new PrescriptionCreatedNotification(e.PrescriptionId, e.OccurredAtUtc),
        PrescriptionCancelledEvent e => new PrescriptionCancelledNotification(e.PrescriptionId, e.OccurredAtUtc),
        PrescriptionRefilledEvent e => new PrescriptionRefilledNotification(e.PrescriptionId, e.OccurredAtUtc),
        PrescriptionDispensedEvent e => new PrescriptionDispensedNotification(
            e.PrescriptionId, e.OccurredAtUtc, e.TotalDispensedQuantity),
        MedicineLowStockEvent e => new MedicineLowStockNotification(
            e.MedicineId, e.MedicineName, e.AvailableStock, e.ReorderLevel, e.OccurredAtUtc),
        MedicineBatchNearExpiryEvent e => new MedicineBatchNearExpiryNotification(
            e.MedicineBatchId, e.MedicineVariantId, e.BatchNumber, e.ExpiryDate, e.OccurredAtUtc),
        _ => throw new NotSupportedException($"No notification mapping for '{domainEvent.GetType().Name}'.")
    };
}