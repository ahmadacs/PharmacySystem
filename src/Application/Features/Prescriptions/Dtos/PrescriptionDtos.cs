namespace Application.Features.Prescriptions.Dtos;

using Domain.Enums;

public sealed record PrescriptionItemDto(
    Guid Id,
    Guid MedicineVariantId,
    string MedicineName,
    string VariantName,
    int PrescribedQuantity,
    int DispensedQuantity,
    int RemainingQuantity,
    string? DosageInstructions,
    bool IsRefillable,
    int RefillsAllowed,
    int RefillsUsed,
    int RefillIntervalDays,
    DateOnly? LastDispensedAt);

/// <summary>
/// Internal EF projection shape for the prescriptions list.
/// Field names mirror <see cref="PrescriptionListItemDto"/>; the doctor name
/// is resolved separately via IStaffService and applied in
/// <c>PrescriptionMapping.ToDto</c>.
/// </summary>
internal sealed record PrescriptionListRow(
    Guid Id,
    Guid DoctorId,
    string PatientName,
    DateOnly PatientDateOfBirth,
    string? PatientPhoneNumber,
    DateOnly IssuedDate,
    PrescriptionStatus Status,
    int ItemCount,
    DateTime CreatedAt);

public sealed record PrescriptionListItemDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    string PatientName,
    DateOnly PatientDateOfBirth,
    int PatientAge,
    string? PatientPhoneNumber,
    DateOnly IssuedDate,
    string Status,
    int ItemCount);

public sealed record PrescriptionDetailsDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    string PatientName,
    DateOnly PatientDateOfBirth,
    int PatientAge,
    string? PatientPhoneNumber,
    string? Diagnosis,
    DateOnly IssuedDate,
    string Status,
    Guid? CreatedBy,
    DateTime CreatedAt,
    IReadOnlyList<PrescriptionItemDto> Items);

/// <summary>
/// Internal EF projection shape for one prescription item.
/// Raw quantities only (Remaining = Prescribed - Dispensed is computed in
/// <c>PrescriptionMapping.ToDto</c> to keep one source of truth).
/// </summary>
internal sealed record PrescriptionDetailsItemRow(
    Guid Id,
    Guid MedicineVariantId,
    string MedicineName,
    MedicineForm? Form,
    MedicineUnit? Unit,
    decimal? Strength,
    int PrescribedQuantity,
    int DispensedQuantity,
    string? DosageInstructions,
    bool IsRefillable,
    int RefillsAllowed,
    int RefillsUsed,
    int RefillIntervalDays,
    DateOnly? LastDispensedAt);

/// <summary>
/// Internal single-query shape for the details screen: header + ordered item rows.
/// </summary>
internal sealed record PrescriptionDetailsRow(
    Guid Id,
    Guid DoctorId,
    string PatientName,
    DateOnly PatientDateOfBirth,
    string? PatientPhoneNumber,
    string? Diagnosis,
    DateOnly IssuedDate,
    PrescriptionStatus Status,
    Guid? CreatedBy,
    DateTime CreatedAt,
    IReadOnlyList<PrescriptionDetailsItemRow> Items);