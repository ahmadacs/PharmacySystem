using Domain.Entities.Prescriptions;
using Domain.Enums;

namespace Application.Features.Prescriptions.Dtos;

public static class PrescriptionMapping
{

    internal static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > today)
            age--;

        return age;
    }

    internal static string BuildVariantName(
        Domain.Enums.MedicineForm? form,
        decimal? strength,
        Domain.Enums.MedicineUnit? unit)
        => form.HasValue && strength.HasValue && unit.HasValue
            ? $"{form.Value} {strength.Value} {unit.Value}"
            : string.Empty;

    internal static PrescriptionListItemDto ToDto(this PrescriptionListRow row, string doctorName)
        => new(
            row.Id,
            row.ShortCode,
            row.DoctorId,
            doctorName,
            row.PatientName,
            row.PatientDateOfBirth,
            CalculateAge(row.PatientDateOfBirth),
            row.PatientPhoneNumber,
            row.IssuedDate,
            row.Status.ToString(),
            row.ItemCount);

    internal static PrescriptionDetailsDto ToDetailsDto(
        this PrescriptionDetailsRow row,
        string doctorName)
    {

        var items = row.Items
            .Select(i => new PrescriptionItemDto(
                i.Id,
                i.MedicineVariantId,
                i.MedicineName,
                i.MedicineNameAr,
                BuildVariantName(i.Form, i.Strength, i.Unit),
                i.PrescribedQuantity,
                i.DispensedQuantity,
                i.PrescribedQuantity - i.DispensedQuantity,
                i.DosageInstructions,
                i.IsRefillable,
                i.RefillsAllowed,
                i.RefillsUsed,
                i.RefillIntervalDays,
                i.LastDispensedAt,
                (int?)i.Form,
                (int?)i.Unit,
                i.Strength))
            .OrderBy(i => i.Id)
            .ToList();

        return new PrescriptionDetailsDto(
            row.Id,
            row.ShortCode,
            row.DoctorId,
            doctorName,
            row.PatientName,
            row.PatientDateOfBirth,
            CalculateAge(row.PatientDateOfBirth),
            row.PatientPhoneNumber,
            row.Diagnosis,
            row.IssuedDate,
            row.Status.ToString(),
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