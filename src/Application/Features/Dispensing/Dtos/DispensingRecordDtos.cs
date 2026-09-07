using Domain.Entities.Dispensing;
using Domain.Enums;

namespace Application.Features.Dispensing.Dtos;

public sealed record DispensingRecordItemDto(
    Guid MedicineBatchId,
    string MedicineName,
    string VariantName,
    string BatchNumber,
    int Quantity);

/// <summary>
/// EF projection row for one dispensing record in the list.
/// Never constructed outside queries; maps via <c>DispensingMapping.ToDto</c>.
/// </summary>
public sealed record DispensingRecordRow(
    Guid Id,
    Guid PrescriptionId,
    string PatientName,
    Guid PharmacistId,
    DateTime DispensedAt,
    string? Notes);

/// <summary>
/// EF projection row for one dispensed line.
/// Never constructed outside queries; maps via <c>DispensingMapping.ToDto</c>.
/// </summary>
public sealed record DispensingRecordItemRow(
    Guid RecordId,
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

    /// <summary>Maps a dispensed-line projection row.</summary>
    public static DispensingRecordItemDto ToDto(this DispensingRecordItemRow i)
        => new(i.MedicineBatchId, i.MedicineName, i.VariantName, i.BatchNumber, i.Quantity);

    /// <summary>Maps a record projection row (names + lines resolved separately).</summary>
    public static DispensingRecordDto ToDto(
        this DispensingRecordRow r,
        string pharmacistName,
        IReadOnlyList<DispensingRecordItemDto> items)
        => new(
            r.Id,
            r.PrescriptionId,
            r.PatientName,
            r.PharmacistId,
            pharmacistName,
            r.DispensedAt,
            r.Notes,
            items);

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