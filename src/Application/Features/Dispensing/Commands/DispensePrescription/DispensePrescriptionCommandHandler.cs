using Application.Common.Interfaces;
using Application.Common.Options;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using Domain.Services;
using MediatR;

namespace Application.Features.Dispensing.Commands;

public sealed class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Guid>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly DispensingDomainService _dispensing;
    private readonly NotificationOptions _notificationOptions;

    public DispensePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow,
        DispensingDomainService dispensing,
        NotificationOptions notificationOptions)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _currentUser = currentUser;
        _staff = staff;
        _uow = uow;
        _dispensing = dispensing;
        _notificationOptions = notificationOptions;
    }

    public async Task<Guid> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var prescription = await _prescriptions.GetByIdWithItemsAsync(req.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Prescription), req.PrescriptionId);

        var userId = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        var pharmacist = await _staff.GetPharmacistAsync(userId, cancellationToken)
            ?? throw new ForbiddenResourceException("Only a Pharmacist can dispense prescriptions.");
        var pharmacistId = pharmacist.Id;

        var variantIds = prescription.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
        var variants = await _medicines.GetForDispensingAsync(variantIds, cancellationToken);
        var byId = variants.ToDictionary(m => m.Id);

        var now = DateTime.UtcNow;
        var record = _dispensing.Dispense(prescription, byId, pharmacistId, now);

        var asOf = DateOnly.FromDateTime(now);
        var medicines = await _medicines.GetMedicinesByVariantIdsForStockCheckAsync(variantIds, cancellationToken);
        foreach (var medicine in medicines)
            medicine.RaiseLowStockEventIfNeeded(asOf);
        foreach (var batch in variants.SelectMany(v => v.Batches))
            batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);

        record.SetNotes(req.Notes);
        _prescriptions.AddDispensingRecord(record);

        await _uow.SaveChangesAsync(cancellationToken);

        return record.Id;
    }
}