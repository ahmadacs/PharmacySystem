using System.Globalization;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Features.Notifications.Events;
using Domain.Enums;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.Features.Inventory.Events;

public sealed class MedicineLowStockNotificationHandler : IDomainEventHandler<MedicineLowStockEvent>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<MedicineLowStockNotificationHandler> _logger;

    public MedicineLowStockNotificationHandler(
        INotificationService notifications,
        ILogger<MedicineLowStockNotificationHandler> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public Task HandleAsync(MedicineLowStockEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Low stock for variant {VariantId} ({VariantName}) of medicine {MedicineId} ({MedicineName}): {AvailableStock} <= {ReorderLevel}",
            domainEvent.MedicineVariantId, domainEvent.VariantName, domainEvent.MedicineId, domainEvent.MedicineName,
            domainEvent.AvailableStock, domainEvent.ReorderLevel);

        var create = new NotificationCreate(
            NotificationType.LowStock,
            "Low stock alert",
            FormattableString.Invariant($"Low stock for {domainEvent.MedicineName} {domainEvent.VariantName}: {domainEvent.AvailableStock} (reorder level {domainEvent.ReorderLevel})."),
            Data: JsonSerializer.Serialize(new { medicineId = domainEvent.MedicineId, variantId = domainEvent.MedicineVariantId }),
            LocalizationKey: "notifications.lowStock",
            LocalizationParamsJson: JsonSerializer.Serialize(new { medicineName = $"{domainEvent.MedicineName} {domainEvent.VariantName}", availableStock = domainEvent.AvailableStock, reorderLevel = domainEvent.ReorderLevel }));

        return _notifications.SendToStaffAsync(create, cancellationToken);
    }
}

public sealed class MedicineBatchNearExpiryNotificationHandler : IDomainEventHandler<MedicineBatchNearExpiryEvent>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<MedicineBatchNearExpiryNotificationHandler> _logger;

    public MedicineBatchNearExpiryNotificationHandler(
        INotificationService notifications,
        ILogger<MedicineBatchNearExpiryNotificationHandler> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public Task HandleAsync(MedicineBatchNearExpiryEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Batch {BatchNumber} (id {BatchId}) of variant {VariantId} near expiry on {ExpiryDate}",
            domainEvent.BatchNumber, domainEvent.MedicineBatchId, domainEvent.MedicineVariantId, domainEvent.ExpiryDate);

        var create = new NotificationCreate(
            NotificationType.NearExpiry,
            "Batch near expiry",
            FormattableString.Invariant($"Batch {domainEvent.BatchNumber} expires on {domainEvent.ExpiryDate:dd/MM/yyyy}."),
            Data: JsonSerializer.Serialize(new { batchId = domainEvent.MedicineBatchId }),
            LocalizationKey: "notifications.nearExpiry",
            LocalizationParamsJson: JsonSerializer.Serialize(new { batchNumber = domainEvent.BatchNumber, expiryDate = domainEvent.ExpiryDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) }));

        return _notifications.SendToStaffAsync(create, cancellationToken);
    }
}