using Application.Common.Interfaces;
using Application.Features.Prescriptions.Common;
using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Medicines;
using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Guid>
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

    public async Task<Guid> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var userId = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        var doctorId = await _staff.GetDoctorIdForUserAsync(userId, cancellationToken)
            ?? throw new ForbiddenResourceException("Only a Doctor can create prescriptions.");

        var patient = await FindOrCreatePatientAsync(req, cancellationToken);
        var prescription = req.ToEntity(doctorId, patient.Id);

        var variantIds = req.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
        var existingVariants = await _medicines.GetVariantsByIdsAsync(variantIds, cancellationToken);
        var existingVariantIds = existingVariants.Select(v => v.Id).ToHashSet();

        foreach (var item in req.Items)
        {
            if (!existingVariantIds.Contains(item.MedicineVariantId))
                throw new EntityNotFoundException(typeof(MedicineVariant), item.MedicineVariantId);

            prescription.AddItem(item.MedicineVariantId, item.Quantity, item.DosageInstructions);
        }

        _prescriptions.Add(prescription);
        await _uow.SaveChangesAsync(cancellationToken);

        return prescription.Id;
    }

    private async Task<Patient> FindOrCreatePatientAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken)
        => await _patients.GetOrCreateAsync(request.PatientFirstName, request.PatientLastName, request.PatientDateOfBirth, request.PatientPhoneNumber, cancellationToken);
}