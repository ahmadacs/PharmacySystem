using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Security;
using Domain.Enums;
using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Notifications.Events;

/// <summary>
/// MediatR notifications mirroring the stock Domain events (see
/// PrescriptionEventHandlers for the rationale: Domain stays dependency-free).
/// </summary>
public sealed record MedicineLowStockNotification(
    Guid MedicineId,
    string MedicineName,
    int AvailableStock,
    int ReorderLevel,
    DateTime OccurredAtUtc)
    : MedicineLowStockEvent(MedicineId, MedicineName, AvailableStock, ReorderLevel, OccurredAtUtc), INotification;

public sealed record MedicineBatchNearExpiryNotification(
    Guid MedicineBatchId,
    Guid MedicineVariantId,
    string BatchNumber,
    DateOnly ExpiryDate,
    DateTime OccurredAtUtc)
    : MedicineBatchNearExpiryEvent(MedicineBatchId, MedicineVariantId, BatchNumber, ExpiryDate, OccurredAtUtc), INotification;

/// <summary>Pushes a low-stock alert to Pharmacists and Admins, persisted per user.</summary>
public sealed class MedicineLowStockNotificationHandler : INotificationHandler<MedicineLowStockNotification>
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

    public Task Handle(MedicineLowStockNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Low stock for medicine {MedicineId} ({MedicineName}): {AvailableStock} <= {ReorderLevel}",
            notification.MedicineId, notification.MedicineName,
            notification.AvailableStock, notification.ReorderLevel);

        var create = new NotificationCreate(
            NotificationType.LowStock,
            "Low stock alert",
            $"Available stock for {notification.MedicineName} is {notification.AvailableStock} (reorder level {notification.ReorderLevel}).",
            Data: JsonSerializer.Serialize(new { medicineId = notification.MedicineId }),
            LocalizationKey: "notifications.lowStock",
            LocalizationParamsJson: JsonSerializer.Serialize(new { medicineName = notification.MedicineName, availableStock = notification.AvailableStock, reorderLevel = notification.ReorderLevel }));

        return SendToStaffAsync(create, cancellationToken);
    }

    private async Task SendToStaffAsync(NotificationCreate create, CancellationToken cancellationToken)
    {
        await _notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);
        await _notifications.SendToRoleAsync(Roles.Admin, create, cancellationToken);
    }
}

/// <summary>Pushes a near-expiry alert to Pharmacists and Admins, persisted per user.</summary>
public sealed class MedicineBatchNearExpiryNotificationHandler : INotificationHandler<MedicineBatchNearExpiryNotification>
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

    public Task Handle(MedicineBatchNearExpiryNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Batch {BatchNumber} (id {BatchId}) of variant {VariantId} near expiry on {ExpiryDate}",
            notification.BatchNumber, notification.MedicineBatchId, notification.MedicineVariantId, notification.ExpiryDate);

        var create = new NotificationCreate(
            NotificationType.NearExpiry,
            "Batch near expiry",
            $"Batch {notification.BatchNumber} expires on {notification.ExpiryDate:dd/MM/yyyy}.",
            Data: JsonSerializer.Serialize(new { batchId = notification.MedicineBatchId }),
            LocalizationKey: "notifications.nearExpiry",
            LocalizationParamsJson: JsonSerializer.Serialize(new { batchNumber = notification.BatchNumber, expiryDate = notification.ExpiryDate.ToString("dd/MM/yyyy") }));

        return SendToStaffAsync(create, cancellationToken);
    }

    private async Task SendToStaffAsync(NotificationCreate create, CancellationToken cancellationToken)
    {
        await _notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);
        await _notifications.SendToRoleAsync(Roles.Admin, create, cancellationToken);
    }
}