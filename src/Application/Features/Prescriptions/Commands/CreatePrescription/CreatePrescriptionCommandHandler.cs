using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
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
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IPatientRepository _patients;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreatePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IPatientRepository patients,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow,
        IAsyncQueryExecutor executor,
        IAttachmentUploadService attachments,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _patients = patients;
        _currentUser = currentUser;
        _staff = staff;
        _uow = uow;
        _executor = executor;
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
        var existingVariants = await _medicines.GetVariantsByIdsAsync(variantIds, cancellationToken);
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

    private async Task<Patient?> FindOrCreatePatientAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        var firstName = request.PatientFirstName.Trim();
        var lastName = request.PatientLastName.Trim();
        var normalizedPhone = NormalizePhone(request.PatientPhoneNumber);

        var patient = await _patients.FindByPhoneAsync(normalizedPhone, cancellationToken);
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