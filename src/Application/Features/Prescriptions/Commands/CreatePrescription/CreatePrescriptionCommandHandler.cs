using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Patients.Dtos;
using Application.Features.Prescriptions.Common;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Commands;

public sealed class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Result<Guid>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IPatientRepository _patients;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreatePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IPatientRepository patients,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow,
        IAsyncQueryExecutor executor,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _patients = patients;
        _currentUser = currentUser;
        _staff = staff;
        _uow = uow;
        _executor = executor;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            var doctorId = await _staff.GetDoctorIdForUserAsync(userId, cancellationToken);
            if (doctorId is null)
                return Result<Guid>.Failure(_localizer["OnlyDoctorCreate"].Value, 403);

            Patient patient;
            try
            {
                patient = await FindOrCreatePatientAsync(req, cancellationToken);
            }
            catch (ConflictingOperationException)
            {
                return Result<Guid>.Failure(_localizer["PhoneRegistered", NormalizePhone(req.PatientPhoneNumber)].Value, 409);
            }
            var prescription = req.ToEntity(doctorId.Value, patient.Id);

            var variantIds = req.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
            var existingVariants = await _medicines.GetVariantsByIdsAsync(variantIds, cancellationToken);
            var existingVariantIds = existingVariants.Select(v => v.Id).ToHashSet();

            foreach (var item in req.Items)
            {
                if (!existingVariantIds.Contains(item.MedicineVariantId))
                    return Result<Guid>.Failure(_localizer["ResourceNotFound", nameof(MedicineVariant), item.MedicineVariantId].Value, 404);

                try
                {
                    prescription.AddItem(item.MedicineVariantId, item.Quantity, item.DosageInstructions, item.IsRefillable, item.RefillsAllowed, item.RefillIntervalDays);
                }
                catch (DomainException ex) when (ex is InvalidPrescriptionStatusException)
                {
                    return Result<Guid>.Failure(ex.Message, 409);
                }
                catch (DomainException ex)
                {
                    return Result<Guid>.Failure(ex.Message, 422);
                }
            }

            _prescriptions.Add(prescription);
            try
            {
                await _uow.SaveChangesAsync(cancellationToken);
            }
            catch (DomainException ex) when (ex is InvalidPrescriptionStatusException or RefillNotEligibleException)
            {
                return Result<Guid>.Failure(ex.Message, 409);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(ex.Message, 422);
            }

            return Result<Guid>.Success(prescription.Id);
        }

        return Result<Guid>.Failure(authResult.Error!, authResult.StatusCode);
    }

    private async Task<Patient> FindOrCreatePatientAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        var firstName = request.PatientFirstName.Trim();
        var lastName = request.PatientLastName.Trim();
        var normalizedPhone = NormalizePhone(request.PatientPhoneNumber);

        var patient = await _patients.FindByPhoneAsync(normalizedPhone, cancellationToken);
        if (patient is not null)
        {
            // Verify identity: phone is unique, but if name/DOB mismatch, it's a different person trying to use same phone
            if (!string.Equals(patient.FirstName, firstName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(patient.LastName, lastName, StringComparison.OrdinalIgnoreCase) ||
                patient.DateOfBirth != request.PatientDateOfBirth)
            {
                throw new ConflictingOperationException($"Phone number {normalizedPhone} is already registered to another patient.");
            }
            return patient;
        }

        // Fallback: check by name+DOB to prevent duplicate patient with different phone
        var byNameDob = await _executor.FirstOrDefaultAsync(
            _patients.Query().Where(p => p.FirstName == firstName && p.LastName == lastName && p.DateOfBirth == request.PatientDateOfBirth),
            cancellationToken);
        if (byNameDob is not null)
        {
            if (byNameDob.PhoneNumber != normalizedPhone)
                byNameDob.UpdatePhone(normalizedPhone);
            return byNameDob;
        }

        var newPatient = PatientMapping.ToEntity(firstName, lastName, request.PatientDateOfBirth, normalizedPhone);
        _patients.Add(newPatient);
        return newPatient;
    }

    private static string NormalizePhone(string phoneNumber)
        => phoneNumber.Trim().Replace(" ", "").Replace("-", "");
}