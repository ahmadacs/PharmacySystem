namespace Application.Features.Dashboard.Dtos;

public sealed record DashboardSummaryDto(
    int TotalMedicines,
    int TotalVariants,
    int PrescriptionsCreatedToday,
    int DispensedToday,
    int AdjustmentsToday,
    int LowStock,
    int ExpiredBatches,
    int ExpiringSoon,
    DateTime GeneratedAt);
