namespace Application.Features.Patients.Dtos;

using Domain.Enums;

/// <summary>
/// One prescribed line inside the patient medication history.
/// <see cref="IsCurrentlyActive"/> is computed server-side so the doctor UI
/// never re-implements business rules (single source of truth).
/// </summary>
public sealed record PatientMedicationItemDto(
    Guid PrescriptionItemId,
    Guid MedicineVariantId,
    Guid MedicineId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    // Raw variant codes so the client localizes via its own dictionaries
    // (server never bakes English display strings as the only source).
    string Form,
    string Unit,
    decimal Strength,
    string? DosageInstructions,
    int PrescribedQuantity,
    int DispensedQuantity,
    int RemainingQuantity,
    bool IsRefillable,
    int RefillsAllowed,
    int RefillsUsed,
    int RefillIntervalDays,
    DateOnly? LastDispensedAt,
    DateOnly? NextEligibleDate,
    bool IsCurrentlyActive);

/// <summary>
/// One prescription inside the lookback window (Cancelled/Expired excluded).
/// </summary>
public sealed record PatientPrescriptionHistoryDto(
    Guid Id,
    DateOnly IssuedDate,
    string Status,
    int ItemCount,
    IReadOnlyList<PatientMedicationItemDto> Items);

/// <summary>
/// Internal query projection shape for one prescribed line (variant info flattened).
/// Computed fields (Remaining, NextEligibleDate, IsCurrentlyActive) live in
/// <c>PatientMedicationMapping</c>.
/// </summary>
internal sealed record PatientMedicationItemRow(
    Guid Id,
    Guid MedicineVariantId,
    Guid MedicineId,
    string MedicineName,
    string? MedicineNameAr,
    MedicineForm Form,
    MedicineUnit Unit,
    decimal Strength,
    string? DosageInstructions,
    int PrescribedQuantity,
    int DispensedQuantity,
    bool IsRefillable,
    int RefillsAllowed,
    int RefillsUsed,
    int RefillIntervalDays,
    DateOnly? LastDispensedAt);

/// <summary>
/// Internal single-query shape for the patient history screen.
/// </summary>
internal sealed record PatientPrescriptionHistoryRow(
    Guid Id,
    DateOnly IssuedDate,
    PrescriptionStatus Status,
    IReadOnlyList<PatientMedicationItemRow> Items);

public static class PatientMedicationMapping
{
    /// <summary>
    /// "Currently taken" rule:
    /// - Prescription must be active (Pending / PartiallyDispensed, or
    ///   FullyDispensed with refills remaining).
    /// - Recency ceiling (tweak #1): the last activity (LastDispensedAt or
    ///   IssuedDate when never dispensed) must be inside the lookback window,
    ///   otherwise an abandoned drug is never shown as current even if a
    ///   refill is theoretically left.
    /// </summary>
    private static bool IsActive(
        string status,
        bool isRefillable,
        int refillsUsed,
        int refillsAllowed,
        DateOnly? lastDispensedAt,
        DateOnly issuedDate,
        DateOnly cutoff)
    {
        var lastActivity = lastDispensedAt ?? issuedDate;
        if (lastActivity < cutoff)
            return false;

        return status switch
        {
            "Pending" or "PartiallyDispensed" => true,
            "FullyDispensed" => isRefillable && refillsUsed < refillsAllowed,
            _ => false,
        };
    }

    private static DateOnly? NextEligible(DateOnly? lastDispensedAt, int intervalDays)
        => lastDispensedAt.HasValue && intervalDays > 0
            ? lastDispensedAt.Value.AddDays(intervalDays)
            : null;

    /// <summary>
    /// Assembles the history DTO from the single-query projection rows
    /// (see GetPatientPrescriptionsQueryHandler).
    /// </summary>
    internal static PatientPrescriptionHistoryDto ToDto(this PatientPrescriptionHistoryRow r, DateOnly cutoff)
    {
        var status = r.Status.ToString();
        var items = r.Items.Select(i => new PatientMedicationItemDto(
            i.Id,
            i.MedicineVariantId,
            i.MedicineId,
            i.MedicineName,
            i.MedicineNameAr,
            $"{i.Form} {i.Strength} {i.Unit}",
            i.Form.ToString(),
            i.Unit.ToString(),
            i.Strength,
            i.DosageInstructions,
            i.PrescribedQuantity,
            i.DispensedQuantity,
            i.PrescribedQuantity - i.DispensedQuantity,
            i.IsRefillable,
            i.RefillsAllowed,
            i.RefillsUsed,
            i.RefillIntervalDays,
            i.LastDispensedAt,
            NextEligible(i.LastDispensedAt, i.RefillIntervalDays),
            IsActive(status, i.IsRefillable, i.RefillsUsed, i.RefillsAllowed, i.LastDispensedAt, r.IssuedDate, cutoff))).ToList();

        return new(r.Id, r.IssuedDate, status, items.Count, items);
    }
}
