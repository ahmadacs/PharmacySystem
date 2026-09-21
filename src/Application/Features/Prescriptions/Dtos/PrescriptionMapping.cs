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
            item.DosageInstructions,
            item.IsRefillable,
            item.RefillsAllowed,
            item.RefillsUsed,
            item.RefillIntervalDays,
            item.LastDispensedAt);

    /// <summary>Maps a list-screen projection row (doctor name resolved separately).</summary>
    public static PrescriptionListItemDto ToDto(this PrescriptionListRow row, string doctorName)
        => new(
            row.Id,
            row.DoctorId,
            doctorName,
            row.PatientName,
            row.PatientDateOfBirth,
            row.PatientAge,
            row.PatientPhone,
            row.IssuedDate,
            row.Status,
            row.ItemCount);

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
            prescription.Status.ToString(),
            prescription.CreatedBy,
            prescription.CreatedAt,
            items);
    }

    public static Prescription ToEntity(this CreatePrescriptionRequest request, Guid doctorId, Guid patientId)
        => new(
            doctorId,
            patientId,
            request.IssuedDate,
            request.Diagnosis);
}