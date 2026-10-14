namespace Application.Features.Dispensing.Dtos;

using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
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
    IReadOnlyList<string> Warnings,
    IReadOnlyList<DispensedItemDto> Items,
    string? NextShortCode = null);

public sealed record DispensedItemDto(
    Guid PrescriptionItemId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    int? Form,
    int? Unit,
    decimal? Strength,
    int PrescribedQuantity,
    int DispensedThisTime,
    int TotalDispensedQuantity,
    bool IsRefillable,
    int RefillsUsed,
    int RefillsRemaining,
    DateOnly? NextRefillEligibleDate);

public static class DispensingMapping
{

    internal static DispensingRecordItemDto ToDto(this DispensingRecordItemRow i)
        => new(i.MedicineBatchId, i.MedicineName, i.MedicineNameAr, PrescriptionMapping.BuildVariantName(i.Form, i.Strength, i.Unit), (int?)i.Form, (int?)i.Unit, i.Strength, i.BatchNumber, i.Quantity);

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

    internal static List<DispensedItemDto> ToDispensedItems(
        IReadOnlyCollection<PrescriptionItem> items,
        IReadOnlyDictionary<Guid, MedicineVariant> variantsById,
        IReadOnlyDictionary<Guid, int> dispensedByItem)
    {
        var result = new List<DispensedItemDto>(items.Count);
        foreach (var item in items)
        {
            variantsById.TryGetValue(item.MedicineVariantId, out var variant);
            result.Add(new DispensedItemDto(
                item.Id,
                variant?.Medicine?.Name ?? "Unknown",
                variant?.Medicine?.NameAr,
                variant is null
                    ? string.Empty
                    : PrescriptionMapping.BuildVariantName(variant.Form, variant.Strength, variant.Unit),
                (int?)variant?.Form,
                (int?)variant?.Unit,
                variant?.Strength,
                item.PrescribedQuantity.Value,
                dispensedByItem.GetValueOrDefault(item.Id),
                item.DispensedQuantity.Value,
                item.IsRefillable,
                item.RefillsUsed,
                item.RefillsRemaining,
                item.NextRefillEligibleDate));
        }

        return result;
    }
}