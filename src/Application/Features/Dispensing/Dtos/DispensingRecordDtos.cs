namespace Application.Features.Dispensing.Dtos;

public sealed record DispensingRecordItemDto(
    Guid MedicineBatchId,
    string MedicineName,
    string VariantName,
    string BatchNumber,
    int Quantity);

/// <summary>
/// Internal EF projection shape for the list. PharmacistName is resolved
/// separately via IStaffService and applied in <c>DispensingMapping.ToDto</c>.
/// </summary>
internal sealed record DispensingRecordRow(
    Guid Id,
    Guid PrescriptionId,
    string PatientName,
    Guid PharmacistId,
    DateTime DispensedAt,
    string? Notes);

/// <summary>
/// Internal EF projection shape for one dispensed line.
/// <c>RecordId</c> exists only for in-memory grouping by record (see handler);
/// it is not part of the API contract.
/// </summary>
internal sealed record DispensingRecordItemRow(
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

/// <summary>
/// Dispense result: the created record id, prominent totals, and non-blocking
/// messages already localized for the request culture (partial shortfall
/// first, then near-expiry batches). Displayed as-is by the frontend.
/// </summary>
public sealed record DispensePrescriptionResponse(
    Guid Id,
    int RequestedQuantity,
    int DispensedQuantity,
    IReadOnlyList<string> Warnings);

public static class DispensingMapping
{
    /// <summary>Maps a dispensed-line projection row.</summary>
    internal static DispensingRecordItemDto ToDto(this DispensingRecordItemRow i)
        => new(i.MedicineBatchId, i.MedicineName, i.VariantName, i.BatchNumber, i.Quantity);

    /// <summary>Maps a record projection row (names + lines resolved separately).</summary>
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