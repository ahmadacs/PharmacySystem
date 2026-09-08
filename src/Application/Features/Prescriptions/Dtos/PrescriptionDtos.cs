namespace Application.Features.Prescriptions.Dtos;

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

public sealed record VariantInfo(string MedicineName, string VariantName);

/// <summary>
/// EF projection row for the prescriptions list.
/// Never constructed outside queries; maps via <c>PrescriptionMapping.ToDto</c>.
/// </summary>
public sealed record PrescriptionListRow(
    Guid Id,
    Guid DoctorId,
    string PatientName,
    DateOnly PatientDateOfBirth,
    int PatientAge,
    string? PatientPhone,
    DateOnly IssuedDate,
    string Status,
    int ItemCount);

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