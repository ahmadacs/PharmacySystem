using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Dispensing.Dtos;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using Domain.Services;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Dispensing.Commands;

public sealed class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result<DispensePrescriptionResponse>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly DispensingDomainService _dispensing;
    private readonly NotificationOptions _notificationOptions;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DispensePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow,
        DispensingDomainService dispensing,
        NotificationOptions notificationOptions,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _currentUser = currentUser;
        _staff = staff;
        _uow = uow;
        _dispensing = dispensing;
        _notificationOptions = notificationOptions;
        _localizer = localizer;
    }

    public async Task<Result<DispensePrescriptionResponse>> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var prescription = await _prescriptions.GetByIdWithItemsAsync(req.PrescriptionId, cancellationToken);
        if (prescription is null)
            return Result<DispensePrescriptionResponse>.Failure($"Resource '{nameof(Prescription)}' with id '{req.PrescriptionId}' was not found.", 404);

        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            var pharmacist = await _staff.GetPharmacistAsync(userId, cancellationToken);
            if (pharmacist is null)
                return Result<DispensePrescriptionResponse>.Failure("Only a Pharmacist can dispense prescriptions.", 403);
            var pharmacistId = pharmacist.Value.Id;

            var variantIds = prescription.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
            var variants = await _medicines.GetForDispensingAsync(variantIds, cancellationToken);
            var byId = variants.ToDictionary(m => m.Id);

            // Total units targeted by this dispense (remaining across pending items).
            var requestedTotal = prescription.Items
                .Where(i => !i.IsFullyDispensed)
                .Sum(i => i.RemainingQuantity.Value);

            var now = DateTime.UtcNow;
            Domain.Entities.Dispensing.DispensingRecord record;
            try
            {
                record = _dispensing.Dispense(prescription, byId, pharmacistId, now);
            }
            catch (DomainException ex) when (ex is InvalidPrescriptionStatusException or RefillNotEligibleException or ConflictingOperationException)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 409);
            }
            catch (DomainException ex) when (ex is MissingMedicineVariantException)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 400);
            }
            catch (InsufficientStockException ex)
            {
                // The exception carries IDs only: resolve display names here so the
                // localized message shows names instead of GUIDs.
                var variant = variants.FirstOrDefault(v => v.Id == ex.MedicineBatchId)
                    ?? variants.FirstOrDefault(v => v.Batches.Any(b => b.Id == ex.MedicineBatchId));
                var batch = variant?.Batches.FirstOrDefault(b => b.Id == ex.MedicineBatchId);
                var name = variant is null ? "Unknown medicine"
                    : $"{variant.Medicine?.Name} {DispensingMapping.GetVariantDisplayName(variant.Form, variant.Unit, variant.Strength)}".Trim();
                var requested = ex.Requested.ToString(CultureInfo.InvariantCulture);
                var available = ex.Available.ToString(CultureInfo.InvariantCulture);
                if (batch is null)
                    return Result<DispensePrescriptionResponse>.Failure(
                        _localizer["InsufficientStockVariant", name, requested, available].Value, 422);
                return Result<DispensePrescriptionResponse>.Failure(
                    _localizer["InsufficientStockBatch", batch.BatchNumber, name, requested, available].Value, 422);
            }
            catch (DomainException ex) when (ex is ExpiredBatchException or FileValidationException)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 422);
            }
            catch (DomainException ex)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 422);
            }

            var asOf = DateOnly.FromDateTime(now);
            foreach (var variant in variants)
                variant.RaiseLowStockEventIfNeeded(asOf);
            foreach (var batch in variants.SelectMany(v => v.Batches))
                batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);

            record.SetNotes(req.Notes);
            _prescriptions.AddDispensingRecord(record);

            try
            {
                await _uow.SaveChangesAsync(cancellationToken);
            }
            catch (DomainException ex) when (ex is InvalidPrescriptionStatusException or RefillNotEligibleException or ConflictingOperationException)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 409);
            }
            catch (DomainException ex) when (ex is MissingMedicineVariantException)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 400);
            }
            catch (DomainException ex) when (ex is InsufficientStockException or ExpiredBatchException or FileValidationException)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 422);
            }
            catch (DomainException ex)
            {
                return Result<DispensePrescriptionResponse>.Failure(ex.Message, 422);
            }

            // Transparency-only messages, already localized for the request culture:
            // the dispense already succeeded with 201, no new rejections.
            var dispensedTotal = record.GetQuantitiesByPrescriptionItem().Values.Sum();
            var warnings = new List<string>();
            if (dispensedTotal < requestedTotal)
                warnings.Add(_localizer["PartialShortfall",
                    dispensedTotal.ToString(CultureInfo.InvariantCulture),
                    requestedTotal.ToString(CultureInfo.InvariantCulture)].Value);

            var usedBatchIds = record.Items.Select(i => i.MedicineBatchId).ToHashSet();
            warnings.AddRange(variants
                .SelectMany(v => v.Batches)
                .Where(b => usedBatchIds.Contains(b.Id)
                    && b.ExpiryDate.DayNumber - asOf.DayNumber <= _notificationOptions.ExpiryWarningDays)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => _localizer["BatchNearExpiry",
                    b.BatchNumber,
                    b.ExpiryDate.ToString("dd/MM/yyyy"),
                    (b.ExpiryDate.DayNumber - asOf.DayNumber).ToString(CultureInfo.InvariantCulture)].Value));

            return Result<DispensePrescriptionResponse>.Success(new(record.Id, requestedTotal, dispensedTotal, warnings));
        }

        return Result<DispensePrescriptionResponse>.Failure(authResult.Error!, authResult.StatusCode);
    }
}