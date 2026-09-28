namespace Application.Features.Dispensing.Dtos;

using Domain.Enums;

public sealed record DispensingRecordItemDto(
    Guid MedicineBatchId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    int? Form,
    int? Unit,
    decimal? Strength,
    string BatchNumber,
    int Quantity);

internal sealed record DispensingRecordRow(
    Guid Id,
    Guid PrescriptionId,
    string PatientName,
    Guid PharmacistId,
    DateTime DispensedAt,
    string? Notes);

internal sealed record DispensingRecordItemRow(
    Guid RecordId,
    Guid MedicineBatchId,
    string MedicineName,
    string? MedicineNameAr,
    MedicineForm? Form,
    MedicineUnit? Unit,
    decimal? Strength,
    string BatchNumber,
    int Quantity);

internal sealed record DispensingRecordWithItems(
    DispensingRecordRow Row,
    IReadOnlyList<DispensingRecordItemRow> Items);

public sealed record DispensingRecordDto(
    Guid Id,
    Guid PrescriptionId,
    string PatientName,
    Guid PharmacistId,
    string PharmacistName,
    DateTime DispensedAt,
    string? Notes,
    IReadOnlyList<DispensingRecordItemDto> Items);

public sealed record DispensePrescriptionResponse(
    Guid Id,
    int RequestedQuantity,
    int DispensedQuantity,
    IReadOnlyList<string> Warnings);

public static class DispensingMapping
{

    internal static DispensingRecordItemDto ToDto(this DispensingRecordItemRow i)
        => new(i.MedicineBatchId, i.MedicineName, i.MedicineNameAr, BuildVariantName(i.Form, i.Strength, i.Unit), (int?)i.Form, (int?)i.Unit, i.Strength, i.BatchNumber, i.Quantity);

    internal static string BuildVariantName(MedicineForm? form, decimal? strength, MedicineUnit? unit)
        => form.HasValue && strength.HasValue && unit.HasValue
            ? $"{form.Value} {strength.Value} {unit.Value}"
            : string.Empty;

    internal static DispensingRecordDto ToDto(
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
}