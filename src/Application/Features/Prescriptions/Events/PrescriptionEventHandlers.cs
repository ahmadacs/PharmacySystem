using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Security;
using Domain.Enums;
using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Prescriptions.Events;

/// <summary>
/// MediatR notifications that mirror the Domain events. The Domain project is
/// dependency-free (Clean Architecture), so the MediatR INotification types live
/// here and the DomainEventDispatcher (Infrastructure) maps each Domain event to
/// its notification before publishing.
/// </summary>
public sealed record PrescriptionCreatedNotification(Guid PrescriptionId, DateTime OccurredAtUtc)
    : PrescriptionCreatedEvent(PrescriptionId, OccurredAtUtc), INotification;

public sealed record PrescriptionCancelledNotification(Guid PrescriptionId, DateTime OccurredAtUtc)
    : PrescriptionCancelledEvent(PrescriptionId, OccurredAtUtc), INotification;

public sealed record PrescriptionRefilledNotification(Guid PrescriptionId, DateTime OccurredAtUtc)
    : PrescriptionRefilledEvent(PrescriptionId, OccurredAtUtc), INotification;

public sealed record PrescriptionDispensedNotification(
    Guid PrescriptionId,
    DateTime OccurredAtUtc,
    int TotalDispensedQuantity)
    : PrescriptionDispensedEvent(PrescriptionId, OccurredAtUtc, TotalDispensedQuantity), INotification;

/// <summary>
/// Logs prescription creation and pushes a SignalR notification to pharmacists.
/// </summary>
public sealed class PrescriptionCreatedNotificationHandler : INotificationHandler<PrescriptionCreatedNotification>
{
    private readonly ILogger<PrescriptionCreatedNotificationHandler> _logger;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionCreatedNotificationHandler(
        ILogger<PrescriptionCreatedNotificationHandler> logger,
        IPrescriptionRepository prescriptions,
        INotificationService notifications)
    {
        _logger = logger;
        _prescriptions = prescriptions;
        _notifications = notifications;
    }

    public async Task Handle(PrescriptionCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Prescription {PrescriptionId} created at {OccurredAtUtc}",
            notification.PrescriptionId, notification.OccurredAtUtc);

        var prescription = await _prescriptions.GetByIdWithItemsAndDoctorAsync(notification.PrescriptionId, cancellationToken);
        if (prescription is null)
            return;

        var patientName = prescription.Patient?.FullName ?? "Unknown patient";
        var create = new NotificationCreate(
            NotificationType.PrescriptionCreated,
            "New prescription",
            $"A new prescription for {patientName} ({prescription.Items.Count} item(s)) has been created.",
            Data: JsonSerializer.Serialize(new { prescriptionId = prescription.Id }),
            LocalizationKey: "notifications.newPrescription",
            LocalizationParamsJson: JsonSerializer.Serialize(new { patientName, count = prescription.Items.Count }));

        await _notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);
    }
}

/// <summary>Logs prescription cancellations.</summary>
public sealed class PrescriptionCancelledNotificationHandler : INotificationHandler<PrescriptionCancelledNotification>
{
    private readonly ILogger<PrescriptionCancelledNotificationHandler> _logger;

    public PrescriptionCancelledNotificationHandler(ILogger<PrescriptionCancelledNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PrescriptionCancelledNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Prescription {PrescriptionId} cancelled at {OccurredAtUtc}",
            notification.PrescriptionId, notification.OccurredAtUtc);
        return Task.CompletedTask;
    }
}

/// <summary>Logs prescription refills.</summary>
public sealed class PrescriptionRefilledNotificationHandler : INotificationHandler<PrescriptionRefilledNotification>
{
    private readonly ILogger<PrescriptionRefilledNotificationHandler> _logger;

    public PrescriptionRefilledNotificationHandler(ILogger<PrescriptionRefilledNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PrescriptionRefilledNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Prescription {PrescriptionId} refilled at {OccurredAtUtc}",
            notification.PrescriptionId, notification.OccurredAtUtc);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Logs dispensing and pushes a SignalR notification to pharmacists and to the
/// prescribing doctor (so they know their prescription was fulfilled).
/// </summary>
public sealed class PrescriptionDispensedNotificationHandler : INotificationHandler<PrescriptionDispensedNotification>
{
    private readonly ILogger<PrescriptionDispensedNotificationHandler> _logger;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionDispensedNotificationHandler(
        ILogger<PrescriptionDispensedNotificationHandler> logger,
        IPrescriptionRepository prescriptions,
        INotificationService notifications)
    {
        _logger = logger;
        _prescriptions = prescriptions;
        _notifications = notifications;
    }

    public async Task Handle(PrescriptionDispensedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Prescription {PrescriptionId} dispensed ({TotalDispensedQuantity} units) at {OccurredAtUtc}",
            notification.PrescriptionId, notification.TotalDispensedQuantity, notification.OccurredAtUtc);

        var prescription = await _prescriptions.GetByIdWithItemsAndDoctorAsync(notification.PrescriptionId, cancellationToken);
        if (prescription is null)
            return;

        var patientName = prescription.Patient?.FullName ?? "Unknown patient";
        var create = new NotificationCreate(
            NotificationType.PrescriptionDispensed,
            "Prescription dispensed",
            $"Prescription for {patientName} has been dispensed.",
            Data: JsonSerializer.Serialize(new { prescriptionId = prescription.Id }),
            LocalizationKey: "notifications.dispensed",
            LocalizationParamsJson: JsonSerializer.Serialize(new { patientName }));

        await _notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);

        var doctorUserId = prescription.Doctor?.UserId;
        if (doctorUserId.HasValue)
            await _notifications.SendToUserAsync(doctorUserId.Value, create, cancellationToken);
    }
}