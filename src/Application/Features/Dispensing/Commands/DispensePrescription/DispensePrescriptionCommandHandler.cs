using System.Globalization;
using Application.Common.Security;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Dispensing.Dtos;
using Application.Resources;
using Domain.Common;
using Domain.Entities.Dispensing;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using Domain.Services;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Dispensing.Commands;

public sealed class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result<DispensePrescriptionResponse>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IBaseRepository<DispensingRecord> _records;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly DispensingDomainService _dispensing;
    private readonly NotificationOptions _notificationOptions;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DispensePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IBaseRepository<DispensingRecord> records,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow,
        DispensingDomainService dispensing,
        NotificationOptions notificationOptions,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _records = records;
        _currentUser = currentUser;
        _staff = staff;
        _uow = uow;
        _dispensing = dispensing;
        _notificationOptions = notificationOptions;
        _localizer = localizer;
    }

    public async Task<Result<DispensePrescriptionResponse>> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var authFailure = AuthGuard.RequireUserId<DispensePrescriptionResponse>(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var pharmacist = await _staff.GetPharmacistAsync(userId, cancellationToken);
        if (pharmacist is null)
            return Result<DispensePrescriptionResponse>.Failure(_localizer["OnlyPharmacistDispense"].Value, 403);

        var shortCode = (request.Request.ShortCode ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedPhone = PhoneNumbers.NormalizeSaudiPhone(request.Request.PhoneNumber);

        var prescription = await _prescriptions.GetForDispensingAsync(shortCode, normalizedPhone, cancellationToken);

        if (prescription is null)
            return Result<DispensePrescriptionResponse>.Failure(
                _localizer["ResourceNotFound", nameof(Prescription), shortCode].Value, 404);

        var requestedTotal = prescription.Items
            .Sum(i => Math.Max(0, i.PrescribedQuantity.Value - i.DispensedQuantity.Value));

        var now = DateTime.UtcNow;
        var byId = prescription.Items
            .Select(i => i.MedicineVariant)
            .Where(v => v is not null)
            .Cast<MedicineVariant>()
            .DistinctBy(v => v.Id)
            .ToDictionary(v => v.Id);

        var record = _dispensing.Dispense(prescription, byId, pharmacist.Value.Id, now);
        record.SetNotes(request.Request.Notes);
        _records.Add(record);

        var asOf = DateOnly.FromDateTime(now);
        foreach (var variant in byId.Values)
        {
            variant.RaiseLowStockEventIfNeeded(asOf, variant.Medicine?.Name);
            foreach (var batch in variant.Batches)
                batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var dispensedTotal = record.GetQuantitiesByPrescriptionItem().Values.Sum();
        List<string> warnings = dispensedTotal < requestedTotal
            ? [_localizer["PartialShortfall",
                dispensedTotal.ToString(CultureInfo.InvariantCulture),
                requestedTotal.ToString(CultureInfo.InvariantCulture)].Value]
            : [];

        return Result<DispensePrescriptionResponse>.Success(
            new(record.Id, requestedTotal, dispensedTotal, warnings));
    }
}