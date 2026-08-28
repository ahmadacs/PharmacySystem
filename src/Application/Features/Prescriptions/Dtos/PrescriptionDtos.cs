namespace Application.Features.Prescriptions.Dtos;

public sealed record PrescriptionItemDto(
    Guid Id,
    Guid MedicineVariantId,
    string MedicineName,
    string VariantName,
    int PrescribedQuantity,
    int DispensedQuantity,
    int RemainingQuantity,
    string? DosageInstructions);

public sealed record VariantInfo(string MedicineName, string VariantName);

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
    bool IsRefillable,
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
    bool IsRefillable,
    int RefillsAllowed,
    int RefillsUsed,
    Guid? CreatedBy,
    DateTime CreatedAt,
    IReadOnlyList<PrescriptionItemDto> Items);