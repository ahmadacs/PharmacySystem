using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Dashboard.Dtos;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IDashboardRepository _dashboard;
    private readonly NotificationOptions _notificationOptions;

    public GetDashboardSummaryQueryHandler(
        IDashboardRepository dashboard,
        NotificationOptions notificationOptions)
    {
        _dashboard = dashboard;
        _notificationOptions = notificationOptions;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var asOf = DateOnly.FromDateTime(now);
        var expiringLimit = asOf.AddDays(_notificationOptions.ExpiryWarningDays);

        var counts = await _dashboard.GetCountsAsync(asOf, expiringLimit, today, tomorrow, cancellationToken);

        return Result<DashboardSummaryDto>.Success(new DashboardSummaryDto(
            counts.TotalMedicines,
            counts.TotalVariants,
            counts.PrescriptionsCreatedToday,
            counts.DispensedToday,
            counts.AdjustmentsToday,
            counts.LowStock,
            counts.ExpiredBatches,
            counts.ExpiringSoon,
            now));
    }
}
