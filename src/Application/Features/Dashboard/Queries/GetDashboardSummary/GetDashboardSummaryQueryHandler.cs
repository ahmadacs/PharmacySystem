using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dashboard.Dtos;
using Application.Features.Dispensing.Queries;
using Application.Features.Inventory.Queries;
using Application.Features.Prescriptions.Queries;
using Domain.Enums;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IMedicineRepository _medicines;
    private readonly IPrescriptionRepository _prescriptions;

    public GetDashboardSummaryQueryHandler(IMedicineRepository medicines, IPrescriptionRepository prescriptions)
    {
        _medicines = medicines;
        _prescriptions = prescriptions;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        // Single API call (one HTTP GET) that aggregates 6 data sets.
        // Sequential awaits to avoid DbContext concurrency (DbContext is not thread-safe).
        var dispensedToday = await _prescriptions.ListDispensingRecordsAsync(
            new DispensingRecordListQuery(Page: 1, PageSize: 1, FromDate: today, ToDate: tomorrow),
            cancellationToken);

        var pending = await _prescriptions.ListAsync(
            new ListPrescriptionsQuery(Page: 1, PageSize: 5, Status: PrescriptionStatus.Pending, SortBy: "issuedDate", SortDir: "desc"),
            restrictedToDoctorId: null,
            cancellationToken);

        var createdToday = await _prescriptions.ListAsync(
            new ListPrescriptionsQuery(Page: 1, PageSize: 1, FromDate: asOf, ToDate: asOf),
            restrictedToDoctorId: null,
            cancellationToken);

        var lowStock = await _medicines.GetLowStockAsync(cancellationToken);

        var expiring = await _medicines.ListBatchesAsync(
            new BatchListQuery(Page: 1, PageSize: 1, ExpiryStatus: "ExpiringSoon", WithinDays: 30),
            cancellationToken);

        var fragmentedList = await _prescriptions.ListAsync(
            new ListPrescriptionsQuery(Page: 1, PageSize: 5, Status: PrescriptionStatus.PartiallyDispensed, SortBy: "issuedDate", SortDir: "desc"),
            restrictedToDoctorId: null,
            cancellationToken);

        var dto = new DashboardSummaryDto(
            DispensedToday: dispensedToday.TotalCount,
            Pending: pending.TotalCount,
            CreatedToday: createdToday.TotalCount,
            LowStock: lowStock.Count,
            ExpiringSoon: expiring.TotalCount,
            Fragmented: fragmentedList.TotalCount,
            GeneratedAt: DateTime.UtcNow,
            LatestPending: pending.Items,
            LatestFragmented: fragmentedList.Items);

        return Result<DashboardSummaryDto>.Success(dto);
    }
}
