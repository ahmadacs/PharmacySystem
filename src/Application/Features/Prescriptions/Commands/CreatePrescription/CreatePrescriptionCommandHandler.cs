using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Patients.Dtos;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Commands;

public sealed class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Result<Guid>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IBaseRepository<MedicineVariant> _variants;
    private readonly IBaseRepository<Patient> _patients;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreatePrescriptionCommandHandler(
        IBaseRepository<Prescription> prescriptions,
        IBaseRepository<MedicineVariant> variants,
        IBaseRepository<Patient> patients,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow,
        IAttachmentUploadService attachments,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _variants = variants;
        _patients = patients;
        _currentUser = currentUser;
        _staff = staff;
        _uow = uow;
        _attachments = attachments;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var authFailure = AuthGuard.RequireUserId<Guid>(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var doctorId = await _staff.GetDoctorIdForUserAsync(userId, cancellationToken);
        if (doctorId is null)
            return Result<Guid>.Failure(_localizer["OnlyDoctorCreate"].Value, 403);

            var patient = await FindOrCreatePatientAsync(req, cancellationToken);
            if (patient is null)
                return Result<Guid>.Failure(_localizer["PhoneRegistered", NormalizePhone(req.PatientPhoneNumber)].Value, 409);

            var prescription = req.ToEntity(doctorId.Value, patient.Id);

        var variantIds = req.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
        var variantsSpec = new Specification<MedicineVariant, MedicineVariant>(v => v);
        variantsSpec.Where(v => variantIds.Contains(v.Id));
        var existingVariants = await _variants.ListAsync(variantsSpec, cancellationToken);
        var existingVariantIds = existingVariants.Select(v => v.Id).ToHashSet();

        foreach (var item in req.Items)
        {
            if (!existingVariantIds.Contains(item.MedicineVariantId))
                return Result<Guid>.Failure(_localizer["ResourceNotFound", nameof(MedicineVariant), item.MedicineVariantId].Value, 404);

            prescription.AddItem(item.MedicineVariantId, item.Quantity, item.DosageInstructions, item.IsRefillable, item.RefillsAllowed, item.RefillIntervalDays);
        }

        _prescriptions.Add(prescription);
        await _uow.SaveChangesAsync(cancellationToken);

            await _attachments.UploadAsync("Prescription", prescription.Id, req.File, cancellationToken);

            return Result<Guid>.Success(prescription.Id);
    }

    private async Task<Patient?> FindByPhoneAsync(string normalizedPhone, CancellationToken cancellationToken)
    {
        // Read-only: this path never mutates the patient (numbers are already
        // normalized by the caller, so plain equality matches the old finder).
        var phoneSpec = new Specification<Patient, Patient>(p => p);
        phoneSpec.Where(p => p.PhoneNumber == normalizedPhone);
        return await _patients.GetAsync(phoneSpec, cancellationToken);
    }

    private async Task<Patient?> FindOrCreatePatientAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        var firstName = request.PatientFirstName.Trim();
        var lastName = request.PatientLastName.Trim();
        var normalizedPhone = NormalizePhone(request.PatientPhoneNumber);

        var patient = await FindByPhoneAsync(normalizedPhone, cancellationToken);
        if (patient is not null)
        {
            if (!string.Equals(patient.FirstName, firstName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(patient.LastName, lastName, StringComparison.OrdinalIgnoreCase) ||
                patient.DateOfBirth != request.PatientDateOfBirth)
            {
                return null;
            }
            return patient;
        }

        // Fallback: check by name+DOB to prevent duplicate patient with different phone.
        // Tracked read: UpdatePhone below must persist on SaveChanges
        // (the old Query() was tracked; specs default to NoTracking).
        var nameDobSpec = new Specification<Patient, Patient>(p => p).Tracked();
        nameDobSpec.Where(p => p.FirstName == firstName && p.LastName == lastName && p.DateOfBirth == request.PatientDateOfBirth);
        var byNameDob = await _patients.GetAsync(nameDobSpec, cancellationToken);
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
        => Domain.Common.PhoneNumbers.NormalizeSaudiPhone(phoneNumber);
}