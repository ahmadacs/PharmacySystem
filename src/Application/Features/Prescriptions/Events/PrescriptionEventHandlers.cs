using System.Linq.Expressions;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Security;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.Features.Prescriptions.Events;

public sealed class PrescriptionCreatedNotificationHandler : IDomainEventHandler<PrescriptionCreatedEvent>
{
    private readonly ILogger<PrescriptionCreatedNotificationHandler> _logger;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionCreatedNotificationHandler(
        ILogger<PrescriptionCreatedNotificationHandler> logger,
        IRepository<Prescription> prescriptions,
        INotificationService notifications)
    {
        _logger = logger;
        _prescriptions = prescriptions;
        _notifications = notifications;
    }

    public async Task HandleAsync(PrescriptionCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Prescription {PrescriptionId} created at {OccurredAtUtc}",
            domainEvent.PrescriptionId, domainEvent.OccurredAtUtc);

        var row = await _prescriptions.GetReadAsync(NotificationRowSpecs.Selector, p => p.Id == domainEvent.PrescriptionId, cancellationToken);
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

public sealed class PrescriptionCancelledNotificationHandler : IDomainEventHandler<PrescriptionCancelledEvent>
{
    private readonly ILogger<PrescriptionCancelledNotificationHandler> _logger;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionCancelledNotificationHandler(
        ILogger<PrescriptionCancelledNotificationHandler> logger,
        IRepository<Prescription> prescriptions,
        INotificationService notifications)
    {
        _logger = logger;
        _prescriptions = prescriptions;
        _notifications = notifications;
    }

    public async Task HandleAsync(PrescriptionCancelledEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Prescription {PrescriptionId} cancelled at {OccurredAtUtc}",
            domainEvent.PrescriptionId, domainEvent.OccurredAtUtc);

        var row = await _prescriptions.GetReadAsync(NotificationRowSpecs.Selector, p => p.Id == domainEvent.PrescriptionId, cancellationToken);
        if (row is null)
            return;

        var create = new NotificationCreate(
            NotificationType.PrescriptionCancelled,
            "Prescription cancelled",
            $"Prescription for {row.PatientName} has been cancelled.",
            Data: JsonSerializer.Serialize(new { prescriptionId = row.Id }),
            LocalizationKey: "notifications.cancelled",
            LocalizationParamsJson: JsonSerializer.Serialize(new { patientName = row.PatientName }));

        await _notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);

        if (row.DoctorUserId.HasValue)
            await _notifications.SendToUserAsync(row.DoctorUserId.Value, create, cancellationToken);
    }
}

public sealed class PrescriptionRefilledNotificationHandler : IDomainEventHandler<PrescriptionRefilledEvent>
{
    private readonly ILogger<PrescriptionRefilledNotificationHandler> _logger;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionRefilledNotificationHandler(
        ILogger<PrescriptionRefilledNotificationHandler> logger,
        IRepository<Prescription> prescriptions,
        INotificationService notifications)
    {
        _logger = logger;
        _prescriptions = prescriptions;
        _notifications = notifications;
    }

    public async Task HandleAsync(PrescriptionRefilledEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Prescription {PrescriptionId} refilled ({ItemCount} item(s)) at {OccurredAtUtc}",
            domainEvent.PrescriptionId, domainEvent.PrescriptionItemIds.Count, domainEvent.OccurredAtUtc);

        var row = await _prescriptions.GetReadAsync(NotificationRowSpecs.Selector, p => p.Id == domainEvent.PrescriptionId, cancellationToken);
        if (row is null)
            return;

        var create = new NotificationCreate(
            NotificationType.PrescriptionRefilled,
            "Prescription refilled",
            $"Prescription for {row.PatientName} refilled ({domainEvent.PrescriptionItemIds.Count} item(s)).",
            Data: JsonSerializer.Serialize(new { prescriptionId = row.Id }),
            LocalizationKey: "notifications.refilled",
            LocalizationParamsJson: JsonSerializer.Serialize(new { patientName = row.PatientName, count = domainEvent.PrescriptionItemIds.Count }));

        await _notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);

        if (row.DoctorUserId.HasValue)
            await _notifications.SendToUserAsync(row.DoctorUserId.Value, create, cancellationToken);
    }
}

public sealed class PrescriptionDispensedNotificationHandler : IDomainEventHandler<PrescriptionDispensedEvent>
{
    private readonly ILogger<PrescriptionDispensedNotificationHandler> _logger;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly INotificationService _notifications;

    public PrescriptionDispensedNotificationHandler(
        ILogger<PrescriptionDispensedNotificationHandler> logger,
        IRepository<Prescription> prescriptions,
        INotificationService notifications)
    {
        _logger = logger;
        _prescriptions = prescriptions;
        _notifications = notifications;
    }

    public async Task HandleAsync(PrescriptionDispensedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Prescription {PrescriptionId} dispensed ({TotalDispensedQuantity} units) at {OccurredAtUtc}",
            domainEvent.PrescriptionId, domainEvent.TotalDispensedQuantity, domainEvent.OccurredAtUtc);

        var row = await _prescriptions.GetReadAsync(NotificationRowSpecs.Selector, p => p.Id == domainEvent.PrescriptionId, cancellationToken);
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

file static class NotificationRowSpecs
{
    public static readonly Expression<Func<Prescription, NotificationPrescriptionRow>> Selector =
        p => new NotificationPrescriptionRow(
            p.Id,
            p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName).Trim() : "Unknown patient",
            p.Items.Count(),
            p.Doctor != null ? (Guid?)p.Doctor.UserId : null);

    public sealed record NotificationPrescriptionRow(Guid Id, string PatientName, int ItemCount, Guid? DoctorUserId);
}
