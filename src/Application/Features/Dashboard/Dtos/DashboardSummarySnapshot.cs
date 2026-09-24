using Domain.Enums;

namespace Application.Features.Dashboard.Dtos;

/// <summary>
/// Internal projection row of the dashboard "latest" lists, loaded by the same
/// database query as the counters (no per-row lookups, no N+1).
/// </summary>
internal sealed record DashboardPrescriptionRow(
    Guid Id,
    Guid DoctorId,
    string PatientName,
    DateOnly PatientDateOfBirth,
    string? PatientPhoneNumber,
    DateOnly IssuedDate,
    PrescriptionStatus Status,
    int ItemCount);

/// <summary>
/// Internal raw dashboard data. Every counter is a scalar subquery and every list a
/// TOP(5) subquery, so the whole snapshot is produced by ONE database round trip.
/// </summary>
internal sealed record DashboardSummarySnapshot(
    int DispensedToday,
    int Pending,
    int CreatedToday,
    int LowStock,
    int ExpiringSoon,
    int Fragmented,
    IReadOnlyList<DashboardPrescriptionRow> LatestPending,
    IReadOnlyList<DashboardPrescriptionRow> LatestFragmented);
