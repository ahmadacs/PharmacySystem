using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Medicines;
using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Result<Guid>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IPatientRepository _patients;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IUnitOfWork _uow;

    public CreatePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IPatientRepository patients,
        ICurrentUserService currentUser,
        IStaffService staff,
        IUnitOfWork uow)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _patients = patients;
        _currentUser = currentUser;
        _staff = staff;
        _uow = uow;
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
                return Result<Guid>.Failure("Only a Doctor can create prescriptions.", 403);

            var patient = await FindOrCreatePatientAsync(req, cancellationToken);
            var prescription = req.ToEntity(doctorId.Value, patient.Id);

            var variantIds = req.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
            var existingVariants = await _medicines.GetVariantsByIdsAsync(variantIds, cancellationToken);
            var existingVariantIds = existingVariants.Select(v => v.Id).ToHashSet();

            foreach (var item in req.Items)
            {
                if (!existingVariantIds.Contains(item.MedicineVariantId))
                    return Result<Guid>.Failure($"Resource '{nameof(MedicineVariant)}' with id '{item.MedicineVariantId}' was not found.", 404);

                try
                {
                    prescription.AddItem(item.MedicineVariantId, item.Quantity, item.DosageInstructions);
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
        => await _patients.GetOrCreateAsync(request.PatientFirstName, request.PatientLastName, request.PatientDateOfBirth, request.PatientPhoneNumber, cancellationToken);
}