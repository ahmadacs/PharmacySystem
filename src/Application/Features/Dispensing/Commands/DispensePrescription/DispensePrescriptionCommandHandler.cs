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
    private readonly IRepository<DispensingRecord> _records;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly DispensingDomainService _dispensing;
    private readonly NotificationOptions _notificationOptions;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DispensePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IRepository<DispensingRecord> records,
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

        var requestedTotal = prescription.Items.Sum(i => i.RemainingQuantity.Value);

        var now = DateTime.UtcNow;
        var variantsById = prescription.Items
            .Select(i => i.MedicineVariant)
            .OfType<MedicineVariant>()
            .DistinctBy(v => v.Id)
            .ToDictionary(v => v.Id);

        var record = _dispensing.Dispense(prescription, variantsById, pharmacist.Value.Id, now);
        record.SetNotes(request.Request.Notes);

        string? nextShortCode = null;
        if (prescription.NeedsShortCodeRotation())
            nextShortCode = await RotateShortCodeAsync(prescription, cancellationToken);

        _records.Add(record);

        var asOf = DateOnly.FromDateTime(now);
        foreach (var variant in variantsById.Values)
        {
            variant.RaiseLowStockEventIfNeeded(asOf, variant.Medicine?.Name);
            foreach (var batch in variant.Batches)
                batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var dispensedByItem = record.GetQuantitiesByPrescriptionItem();
        var dispensedTotal = dispensedByItem.Values.Sum();
        List<string> warnings = dispensedTotal < requestedTotal
            ? [_localizer["PartialShortfall",
                dispensedTotal.ToString(CultureInfo.InvariantCulture),
                requestedTotal.ToString(CultureInfo.InvariantCulture)].Value]
            : [];

        var items = DispensingMapping.ToDispensedItems(prescription.Items, variantsById, dispensedByItem);

        return Result<DispensePrescriptionResponse>.Success(
            new(record.Id, requestedTotal, dispensedTotal, warnings, items, nextShortCode));
    }

    private async Task<string> RotateShortCodeAsync(Prescription prescription, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;

        prescription.RegenerateShortCode();

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (!await _prescriptions.ExistsAsync(
                    p => p.ShortCode == prescription.ShortCode && p.Id != prescription.Id,
                    cancellationToken))
                return prescription.ShortCode;

            prescription.RegenerateShortCode();
        }

        return prescription.ShortCode;
    }
}