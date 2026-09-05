using Application.Features.Prescriptions.Dtos;

namespace Application.Features.Dashboard.Dtos;

public sealed record DashboardSummaryDto(
    int DispensedToday,
    int Pending,
    int CreatedToday,
    int LowStock,
    int ExpiringSoon,
    int Fragmented,
    DateTime GeneratedAt,
    IReadOnlyList<PrescriptionListItemDto> LatestPending,
    IReadOnlyList<PrescriptionListItemDto> LatestFragmented);
