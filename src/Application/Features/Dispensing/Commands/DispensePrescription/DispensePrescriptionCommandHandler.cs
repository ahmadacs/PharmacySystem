using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using Domain.Services;
using MediatR;

namespace Application.Features.Dispensing.Commands;

public sealed class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result<Guid>>
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

    public async Task<Result<Guid>> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var prescription = await _prescriptions.GetByIdWithItemsAsync(req.PrescriptionId, cancellationToken);
        if (prescription is null)
            return Result<Guid>.Failure($"Resource '{nameof(Prescription)}' with id '{req.PrescriptionId}' was not found.", 404);

        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            var pharmacist = await _staff.GetPharmacistAsync(userId, cancellationToken);
            if (pharmacist is null)
                return Result<Guid>.Failure("Only a Pharmacist can dispense prescriptions.", 403);
            var pharmacistId = pharmacist.Value.Id;

            var variantIds = prescription.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
            var variants = await _medicines.GetForDispensingAsync(variantIds, cancellationToken);
            var byId = variants.ToDictionary(m => m.Id);

            var now = DateTime.UtcNow;
            Domain.Entities.Dispensing.DispensingRecord record;
            try
            {
                record = _dispensing.Dispense(prescription, byId, pharmacistId, now);
            }
            catch (DomainException ex) when (ex is InvalidPrescriptionStatusException or RefillNotEligibleException or ConflictingOperationException)
            {
                return Result<Guid>.Failure(ex.Message, 409);
            }
            catch (DomainException ex) when (ex is MissingMedicineVariantException)
            {
                return Result<Guid>.Failure(ex.Message, 400);
            }
            catch (DomainException ex) when (ex is InsufficientStockException or ExpiredBatchException or FileValidationException)
            {
                return Result<Guid>.Failure(ex.Message, 422);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message, 422);
            }

            var asOf = DateOnly.FromDateTime(now);
            var medicines = await _medicines.GetMedicinesByVariantIdsForStockCheckAsync(variantIds, cancellationToken);
            foreach (var medicine in medicines)
                medicine.RaiseLowStockEventIfNeeded(asOf);
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
                return Result<Guid>.Failure(ex.Message, 409);
            }
            catch (DomainException ex) when (ex is MissingMedicineVariantException)
            {
                return Result<Guid>.Failure(ex.Message, 400);
            }
            catch (DomainException ex) when (ex is InsufficientStockException or ExpiredBatchException or FileValidationException)
            {
                return Result<Guid>.Failure(ex.Message, 422);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message, 422);
            }

            return Result<Guid>.Success(record.Id);
        }

        return Result<Guid>.Failure(authResult.Error!, authResult.StatusCode);
    }
}