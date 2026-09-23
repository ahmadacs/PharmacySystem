using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Security;
using Application.Common.Specifications;
using Domain.Entities.Prescriptions;
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

public sealed record PrescriptionRefilledNotification(
    Guid PrescriptionId,
    IReadOnlyList<Guid> PrescriptionItemIds,
    DateTime OccurredAtUtc)
    : PrescriptionRefilledEvent(PrescriptionId, PrescriptionItemIds, OccurredAtUtc), INotification;

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
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionCreatedNotificationHandler(
        ILogger<PrescriptionCreatedNotificationHandler> logger,
        IBaseRepository<Prescription> prescriptions,
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

        var row = await _prescriptions.GetAsync(NotificationRowSpecs.ById(notification.PrescriptionId), cancellationToken);
        if (row is null)
            return;

        var create = new NotificationCreate(
            NotificationType.PrescriptionCreated,
            "New prescription",
            $"A new prescription for {row.PatientName} ({row.ItemCount} item(s)) has been created.",
            Data: JsonSerializer.Serialize(new { prescriptionId = row.Id }),
            LocalizationKey: "notifications.newPrescription",
            LocalizationParamsJson: JsonSerializer.Serialize(new { patientName = row.PatientName, count = row.ItemCount }));

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
        _logger.LogInformation("Prescription {PrescriptionId} refilled ({ItemCount} item(s)) at {OccurredAtUtc}",
            notification.PrescriptionId, notification.PrescriptionItemIds.Count, notification.OccurredAtUtc);
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
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionDispensedNotificationHandler(
        ILogger<PrescriptionDispensedNotificationHandler> logger,
        IBaseRepository<Prescription> prescriptions,
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

        var row = await _prescriptions.GetAsync(NotificationRowSpecs.ById(notification.PrescriptionId), cancellationToken);
        if (row is null)
            return;

        var create = new NotificationCreate(
            NotificationType.PrescriptionDispensed,
            "Prescription dispensed",
            $"Prescription for {row.PatientName} has been dispensed.",
            Data: JsonSerializer.Serialize(new { prescriptionId = row.Id }),
            LocalizationKey: "notifications.dispensed",
            LocalizationParamsJson: JsonSerializer.Serialize(new { patientName = row.PatientName }));

        await _notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);

        if (row.DoctorUserId.HasValue)
            await _notifications.SendToUserAsync(row.DoctorUserId.Value, create, cancellationToken);
    }
}

/// <summary>
/// Read-only projection shared by the notification handlers: exactly the
/// fields notifications need (patient display name with the same
/// "Unknown patient" fallback, item count, doctor user id). Single query,
/// no Include — navigations inside a Select need none.
/// </summary>
file static class NotificationRowSpecs
{
    public static Specification<Prescription, NotificationPrescriptionRow> ById(Guid prescriptionId)
    {
        var spec = new Specification<Prescription, NotificationPrescriptionRow>(p => new NotificationPrescriptionRow(
            p.Id,
            p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName).Trim() : "Unknown patient",
            p.Items.Count(),
            p.Doctor != null ? (Guid?)p.Doctor.UserId : null));
        spec.Where(p => p.Id == prescriptionId);
        return spec;
    }

    public sealed record NotificationPrescriptionRow(Guid Id, string PatientName, int ItemCount, Guid? DoctorUserId);
}