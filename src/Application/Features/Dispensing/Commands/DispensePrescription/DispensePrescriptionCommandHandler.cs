using System.Globalization;
using Application.Common.Security;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Common.Specifications;
using Application.Features.Dispensing.Dtos;
using Application.Resources;
using Domain.Entities.Dispensing;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using Domain.Services;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Dispensing.Commands;

public sealed class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result<DispensePrescriptionResponse>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IBaseRepository<PrescriptionItem> _items;
    private readonly IBaseRepository<MedicineVariant> _variants;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<DispensingRecord> _records;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly DispensingDomainService _dispensing;
    private readonly NotificationOptions _notificationOptions;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DispensePrescriptionCommandHandler(
        IBaseRepository<Prescription> prescriptions,
        IBaseRepository<PrescriptionItem> items,
        IBaseRepository<MedicineVariant> variants,
        IBaseRepository<MedicineBatch> batches,
        IBaseRepository<Medicine> medicines,
        IBaseRepository<DispensingRecord> records,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow,
        DispensingDomainService dispensing,
        NotificationOptions notificationOptions,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _items = items;
        _variants = variants;
        _batches = batches;
        _medicines = medicines;
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
        var req = request.Request;

        // Tracked loads assembled by EF relationship fix-up (no Include):
        // the root first, then each collection; fix-up populates
        // prescription.Items, variant.Batches and variant.Medicine in memory.
        // Everything below mutates, so all loads are tracked.
        var prescriptionSpec = new Specification<Prescription, Prescription>(p => p).Tracked();
        prescriptionSpec.Where(p => p.Id == req.PrescriptionId);
        var prescription = await _prescriptions.GetAsync(prescriptionSpec, cancellationToken);
        if (prescription is null)
            return Result<DispensePrescriptionResponse>.Failure(_localizer["ResourceNotFound", nameof(Prescription), req.PrescriptionId].Value, 404);

        var itemsSpec = new Specification<PrescriptionItem, PrescriptionItem>(i => i).Tracked();
        itemsSpec.Where(i => i.PrescriptionId == req.PrescriptionId);
        itemsSpec.Order(q => q.OrderBy(i => i.Id));
        await _items.ListAsync(itemsSpec, cancellationToken);

        var authFailure = AuthGuard.RequireUserId<DispensePrescriptionResponse>(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var pharmacist = await _staff.GetPharmacistAsync(userId, cancellationToken);
        if (pharmacist is null)
            return Result<DispensePrescriptionResponse>.Failure(_localizer["OnlyPharmacistDispense"].Value, 403);
        var pharmacistId = pharmacist.Value.Id;

        var variantIds = prescription.Items.Select(i => i.MedicineVariantId).Distinct().ToList();

        var variantsSpec = new Specification<MedicineVariant, MedicineVariant>(v => v).Tracked();
        variantsSpec.Where(v => variantIds.Contains(v.Id));
        var variants = await _variants.ListAsync(variantsSpec, cancellationToken);

        var batchesSpec = new Specification<MedicineBatch, MedicineBatch>(b => b).Tracked();
        batchesSpec.Where(b => variantIds.Contains(b.MedicineVariantId));
        await _batches.ListAsync(batchesSpec, cancellationToken);

        var medicineIds = variants.Select(v => v.MedicineId).Distinct().ToList();
        var medicinesSpec = new Specification<Medicine, Medicine>(m => m).Tracked();
        medicinesSpec.Where(m => medicineIds.Contains(m.Id));
        await _medicines.ListAsync(medicinesSpec, cancellationToken);

        var byId = variants.ToDictionary(m => m.Id);

        // Total units targeted by this dispense (remaining across pending items).
        var requestedTotal = prescription.Items
            .Where(i => !i.IsFullyDispensed)
            .Sum(i => i.RemainingQuantity.Value);

        var now = DateTime.UtcNow;
        var record = _dispensing.Dispense(prescription, byId, pharmacistId, now);

        var asOf = DateOnly.FromDateTime(now);
        foreach (var variant in variants)
            variant.RaiseLowStockEventIfNeeded(asOf);
        foreach (var batch in variants.SelectMany(v => v.Batches))
            batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);

        record.SetNotes(req.Notes);
        _records.Add(record);

        await _uow.SaveChangesAsync(cancellationToken);

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
}