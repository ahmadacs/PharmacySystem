namespace Application.Common.Interfaces;

public sealed record DashboardCounts(
    int TotalMedicines,
    int TotalVariants,
    int PrescriptionsCreatedToday,
    int DispensedToday,
    int AdjustmentsToday,
    int LowStock,
    int ExpiredBatches,
    int ExpiringSoon);

public interface IDashboardRepository
{
    /// <summary>
    /// Returns all dashboard counters in a single database roundtrip
    /// (one SELECT with scalar subqueries).
    /// </summary>
    Task<DashboardCounts> GetCountsAsync(
        DateOnly asOf,
        DateOnly expiringLimit,
        DateTime today,
        DateTime tomorrow,
        CancellationToken cancellationToken = default);
}
