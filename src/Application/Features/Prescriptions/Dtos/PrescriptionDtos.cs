namespace Application.Features.Prescriptions.Dtos;

using Domain.Enums;

public sealed record PrescriptionItemDto(
    Guid Id,
    Guid MedicineVariantId,
    string MedicineName,
    string? MedicineNameAr,
    string VariantName,
    int PrescribedQuantity,
    int DispensedQuantity,
    int RemainingQuantity,
    string? DosageInstructions,
    bool IsRefillable,
    int RefillsAllowed,
    int RefillsUsed,
    int RefillIntervalDays,
    DateOnly? LastDispensedAt,
    int? Form,
    int? Unit,
    decimal? Strength);

internal sealed record PrescriptionListRow(
    Guid Id,
    string ShortCode,
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
    string ShortCode,
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
    string ShortCode,
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

internal sealed record PrescriptionDetailsItemRow(
    Guid Id,
    Guid MedicineVariantId,
    string MedicineName,
    string? MedicineNameAr,
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

internal sealed record PrescriptionDetailsRow(
    Guid Id,
    string ShortCode,
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