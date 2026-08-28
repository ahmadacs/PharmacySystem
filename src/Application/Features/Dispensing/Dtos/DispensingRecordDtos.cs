using Domain.Entities.Dispensing;
using Domain.Enums;

namespace Application.Features.Dispensing.Dtos;

public sealed record DispensingRecordItemDto(
    Guid MedicineBatchId,
    string MedicineName,
    string VariantName,
    string BatchNumber,
    int Quantity);

public sealed record DispensingRecordDto(
    Guid Id,
    Guid PrescriptionId,
    string PatientName,
    Guid PharmacistId,
    string PharmacistName,
    DateTime DispensedAt,
    string? Notes,
    IReadOnlyList<DispensingRecordItemDto> Items);

public static class DispensingMapping
{
    public static string GetVariantDisplayName(MedicineForm form, MedicineUnit unit, decimal strength)
        => $"{form} {strength} {unit}";

    public static DispensingRecordDto ToDto(this DispensingRecord record, string patientName, string pharmacistName)
    {
        var items = record.Items
            .Select(i =>
            {
                var variant = i.MedicineBatch?.MedicineVariant;
                var variantDisplayName = variant is not null
                    ? GetVariantDisplayName(variant.Form, variant.Unit, variant.Strength)
                    : string.Empty;

                return new DispensingRecordItemDto(
                    i.MedicineBatchId,
                    i.MedicineBatch is not null && i.MedicineBatch.MedicineVariant is not null && i.MedicineBatch.MedicineVariant.Medicine is not null
                        ? i.MedicineBatch.MedicineVariant.Medicine.Name
                        : "Unknown",
                    variantDisplayName,
                    i.MedicineBatch?.BatchNumber ?? string.Empty,
                    i.Quantity.Value);
            })
            .ToList();

        return new DispensingRecordDto(
            record.Id,
            record.PrescriptionId,
            patientName,
            record.PharmacistId,
            pharmacistName,
            record.DispensedAt,
            record.Notes,
            items);
    }
}