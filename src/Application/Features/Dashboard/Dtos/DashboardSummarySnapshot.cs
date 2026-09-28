using Domain.Enums;

namespace Application.Features.Dashboard.Dtos;

internal sealed record DashboardPrescriptionRow(
    Guid Id,
    string ShortCode,
    Guid DoctorId,
    string PatientName,
    DateOnly PatientDateOfBirth,
    string? PatientPhoneNumber,
    DateOnly IssuedDate,
    PrescriptionStatus Status,
    int ItemCount);

internal sealed record DashboardSummarySnapshot(
    int DispensedToday,
    int Pending,
    int CreatedToday,
    int LowStock,
    int ExpiringSoon,
    int Fragmented,
    IReadOnlyList<DashboardPrescriptionRow> LatestPending,
    IReadOnlyList<DashboardPrescriptionRow> LatestFragmented);
