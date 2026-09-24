using Domain.Entities.Prescriptions;
using Domain.Enums;

namespace Application.Features.Prescriptions.Dtos;

public static class PrescriptionMapping
{
    /// <summary>Maps a list-screen projection row (doctor name resolved separately).</summary>
    internal static PrescriptionListItemDto ToDto(this PrescriptionListRow row, string doctorName)
        => new(
            row.Id,
            row.DoctorId,
            doctorName,
            row.PatientName,
            row.PatientDateOfBirth,
            row.PatientAge,
            row.PatientPhoneNumber,
            row.IssuedDate,
            row.Status,
            row.ItemCount);

    internal static PrescriptionDetailsDto ToDetailsDto(
        this PrescriptionDetailsRow row,
        string doctorName)
    {
        // Remaining mirrors PrescriptionItem.RemainingQuantity
        // (PrescribedQuantity - DispensedQuantity).
        var items = row.Items
            .Select(i => new PrescriptionItemDto(
                i.Id,
                i.MedicineVariantId,
                i.MedicineName,
                i.VariantName,
                i.PrescribedQuantity,
                i.DispensedQuantity,
                i.PrescribedQuantity - i.DispensedQuantity,
                i.DosageInstructions,
                i.IsRefillable,
                i.RefillsAllowed,
                i.RefillsUsed,
                i.RefillIntervalDays,
                i.LastDispensedAt))
            .OrderBy(i => i.Id)
            .ToList();

        return new PrescriptionDetailsDto(
            row.Id,
            row.DoctorId,
            doctorName,
            row.PatientName,
            row.PatientDateOfBirth,
            row.PatientAge,
            row.PatientPhoneNumber,
            row.Diagnosis,
            row.IssuedDate,
            row.Status,
            row.CreatedBy,
            row.CreatedAt,
            items);
    }

    public static Prescription ToEntity(this CreatePrescriptionRequest request, Guid doctorId, Guid patientId)
        => new(
            doctorId,
            patientId,
            request.IssuedDate,
            request.Diagnosis);
}