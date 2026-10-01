using Application.Features.Prescriptions.Dtos;

namespace Application.Features.Dispensing.Dtos;

internal sealed record DispensingLookupItemRow(
    Guid PrescriptionItemId,
    string MedicineName,
    string? MedicineNameAr,
    Domain.Enums.MedicineForm? Form,
    Domain.Enums.MedicineUnit? Unit,
    decimal? Strength,
    int PrescribedQuantity,
    int DispensedQuantity,
    string? DosageInstructions,
    int AvailableStock);

internal sealed record DispensingLookupRow(
    Guid PrescriptionId,
    string ShortCode,
    string PatientName,
    DateOnly IssuedDate,
    Domain.Enums.PrescriptionStatus Status,
    IReadOnlyList<DispensingLookupItemRow> Items);

internal static class DispensingLookupMapping
{
    public static DispensingLookupItemDto ToDto(this DispensingLookupItemRow row)
        => new(
            row.PrescriptionItemId,
            row.MedicineName,
            row.MedicineNameAr,
            PrescriptionMapping.BuildVariantName(row.Form, row.Strength, row.Unit),
            (int?)row.Form,
            (int?)row.Unit,
            row.Strength,
            row.DosageInstructions,
            row.PrescribedQuantity,
            row.DispensedQuantity,
            row.PrescribedQuantity - row.DispensedQuantity,
            row.AvailableStock);

    public static DispensingLookupResponse ToResponse(this DispensingLookupRow row, IReadOnlyList<DispensingLookupItemDto> items)
        => new(
            row.PrescriptionId,
            row.ShortCode,
            row.PatientName,
            row.IssuedDate,
            row.Status.ToString(),
            items);
}

public sealed record DispensingLookupItemDto(
    Guid PrescriptionItemId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    int? Form,
    int? Unit,
    decimal? Strength,
    string? DosageInstructions,
    int PrescribedQuantity,
    int DispensedQuantity,
    int RemainingQuantity,
    int AvailableQuantity);

public sealed record DispensingLookupResponse(
    Guid PrescriptionId,
    string ShortCode,
    string PatientName,
    DateOnly IssuedDate,
    string Status,
    IReadOnlyList<DispensingLookupItemDto> Items);
