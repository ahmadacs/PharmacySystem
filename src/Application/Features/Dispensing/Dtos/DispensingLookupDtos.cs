namespace Application.Features.Dispensing.Dtos;

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
