namespace Application.Features.Patients.Dtos;

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
    public static bool IsActive(
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

    public static DateOnly? NextEligible(DateOnly? lastDispensedAt, int intervalDays)
        => lastDispensedAt.HasValue && intervalDays > 0
            ? lastDispensedAt.Value.AddDays(intervalDays)
            : null;
}
