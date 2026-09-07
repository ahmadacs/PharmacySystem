using Domain.Enums;

namespace Application.Features.Dashboard.Dtos;

/// <summary>
/// One prescription row of the dashboard "latest" lists, loaded by the same
/// database query as the counters (no per-row lookups, no N+1).
/// </summary>
public sealed record DashboardPrescriptionRow(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    string PatientName,
    DateOnly PatientDateOfBirth,
    string? PatientPhoneNumber,
    DateOnly IssuedDate,
    PrescriptionStatus Status,
    bool IsRefillable,
    int ItemCount);

/// <summary>
/// Raw dashboard data. Every counter is a scalar subquery and every list a
/// TOP(5) subquery, so the whole snapshot is produced by ONE database round trip.
/// </summary>
public sealed record DashboardSummarySnapshot(
    int DispensedToday,
    int Pending,
    int CreatedToday,
    int LowStock,
    int ExpiringSoon,
    int Fragmented,
    IReadOnlyList<DashboardPrescriptionRow> LatestPending,
    IReadOnlyList<DashboardPrescriptionRow> LatestFragmented);
