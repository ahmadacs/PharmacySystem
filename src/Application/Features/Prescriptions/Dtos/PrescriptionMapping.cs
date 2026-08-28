using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using Domain.Enums;

namespace Application.Features.Prescriptions.Dtos;

public static class PrescriptionMapping
{
    public static PrescriptionItemDto ToDto(this PrescriptionItem item, string medicineName, string variantName)
        => new(
            item.Id,
            item.MedicineVariantId,
            medicineName,
            variantName,
            item.PrescribedQuantity.Value,
            item.DispensedQuantity.Value,
            item.RemainingQuantity.Value,
            item.DosageInstructions);

    public static PrescriptionListItemDto ToListItemDto(this Prescription prescription, string doctorName)
        => new(
            prescription.Id,
            prescription.DoctorId,
            doctorName,
            prescription.Patient?.FullName ?? string.Empty,
            prescription.Patient?.DateOfBirth ?? default,
            prescription.Patient?.Age ?? 0,
            prescription.Patient?.PhoneNumber,
            prescription.IssuedDate,
            prescription.Status.ToDisplayValue(),
            prescription.IsRefillable,
            prescription.Items.Count);

    public static PrescriptionDetailsDto ToDetailsDto(
        this Prescription prescription,
        string doctorName,
        IReadOnlyDictionary<Guid, VariantInfo> variantInfosById)
    {
        var items = prescription.Items
            .Select(i =>
            {
                var info = variantInfosById.TryGetValue(i.MedicineVariantId, out var value)
                    ? value
                    : new VariantInfo("Unknown", string.Empty);
                return i.ToDto(info.MedicineName, info.VariantName);
            })
            .OrderBy(i => i.Id)
            .ToList();

        return new PrescriptionDetailsDto(
            prescription.Id,
            prescription.DoctorId,
            doctorName,
            prescription.Patient?.FullName ?? string.Empty,
            prescription.Patient?.DateOfBirth ?? default,
            prescription.Patient?.Age ?? 0,
            prescription.Patient?.PhoneNumber,
            prescription.Diagnosis,
            prescription.IssuedDate,
            prescription.Status.ToDisplayValue(),
            prescription.IsRefillable,
            prescription.RefillsAllowed,
            prescription.RefillsUsed,
            prescription.CreatedBy,
            prescription.CreatedAt,
            items);
    }

    public static Prescription ToEntity(this CreatePrescriptionRequest request, Guid doctorId, Guid patientId)
        => new(
            doctorId,
            patientId,
            request.IssuedDate,
            request.Diagnosis,
            request.IsRefillable,
            request.RefillsAllowed);

    public static Patient ToPatient(this CreatePrescriptionRequest request)
        => new(
            request.PatientFirstName,
            request.PatientLastName,
            request.PatientDateOfBirth,
            request.PatientPhoneNumber);

    private static string ToDisplayValue(this PrescriptionStatus status) => status.ToString();
}