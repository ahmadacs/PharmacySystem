using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dashboard.Dtos;
using Application.Features.Prescriptions.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IStaffService _staff;
    private readonly IAsyncQueryExecutor _executor;

    public GetDashboardSummaryQueryHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IStaffService staff,
        IAsyncQueryExecutor executor)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _staff = staff;
        _executor = executor;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var asOf = DateOnly.FromDateTime(now);
        var expiringLimit = asOf.AddDays(30);

        // Raw sets only: repositories expose data, every rule below lives here
        // in the Application layer (same predicates as the list-query handlers).
        var dispensing = _prescriptions.QueryDispensingRecords();
        var prescriptions = _prescriptions.Query();
        var medicines = _medicines.Query();
        var batches = _medicines.QueryBatches();

        // Anchor: one prescription row, so every aggregate below runs as a
        // subquery of a SINGLE SELECT (EF needs a keyed parent to shape the
        // TOP(5) collection subqueries). Pure plumbing: no thresholds, no statuses.
        // Empty-prescriptions case is handled by the fallback below.
        var anchor = prescriptions.Take(1);

        // Query 1/2: all counters as scalar subqueries + both TOP(5) lists.
        // Doctor names stay out: Identity tables are invisible to this layer,
        // so names resolve with ONE batched lookup below (no N+1).
        var snapshot = await _executor.SingleOrDefaultAsync(
            from _ in anchor
            select new DashboardSummarySnapshot(
                // Same predicate as DispensingRecordListQueryHandler (inclusive range).
                DispensedToday: dispensing
                    .Count(r => r.DispensedAt >= today && r.DispensedAt <= tomorrow),
                Pending: prescriptions
                    .Count(p => p.Status == PrescriptionStatus.Pending),
                CreatedToday: prescriptions
                    .Count(p => p.IssuedDate == asOf),
                // Same rule as ListLowStockQueryHandler: available non-expired
                // stock at or below the variant reorder level.
                LowStock: medicines
                    .Where(m => m.IsActive)
                    .SelectMany(m => m.Variants
                        .Where(v => v.IsActive && !v.IsDeleted)
                        .Select(v => new
                        {
                            Available = v.Batches
                                .Where(b => !b.IsDeleted && b.ExpiryDate > asOf)
                                .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                            ReorderLevel = v.ReorderLevel.Value
                        }))
                    .Count(x => x.Available <= x.ReorderLevel),
                // Same window as BatchListQueryHandler "ExpiringSoon" (WithinDays = 30).
                ExpiringSoon: batches
                    .Count(b => b.ExpiryDate > asOf && b.ExpiryDate <= expiringLimit),
                Fragmented: prescriptions
                    .Count(p => p.Status == PrescriptionStatus.PartiallyDispensed),
                // Written out twice because EF cannot translate a helper-method
                // call inside the expression tree.
                LatestPending: (from p in prescriptions
                                where p.Status == PrescriptionStatus.Pending
                                orderby p.IssuedDate descending
                                select new DashboardPrescriptionRow(
                                    p.Id,
                                    p.DoctorId,
                                    string.Empty,
                                    p.Patient == null ? string.Empty : (p.Patient.FirstName + " " + p.Patient.LastName).Trim(),
                                    p.Patient != null ? p.Patient.DateOfBirth : default,
                                    p.Patient != null ? p.Patient.PhoneNumber : null,
                                    p.IssuedDate,
                                    p.Status,
                                    p.Items.Count))
                    .Take(5)
                    .ToList(),
                LatestFragmented: (from p in prescriptions
                                   where p.Status == PrescriptionStatus.PartiallyDispensed
                                   orderby p.IssuedDate descending
                                   select new DashboardPrescriptionRow(
                                       p.Id,
                                       p.DoctorId,
                                       string.Empty,
                                       p.Patient == null ? string.Empty : (p.Patient.FirstName + " " + p.Patient.LastName).Trim(),
                                       p.Patient != null ? p.Patient.DateOfBirth : default,
                                       p.Patient != null ? p.Patient.PhoneNumber : null,
                                       p.IssuedDate,
                                       p.Status,
                                       p.Items.Count))
                    .Take(5)
                    .ToList()),
            cancellationToken)
            ?? await PrescriptionlessFallbackAsync(cancellationToken);

        // Query 2/2: every distinct doctor of both lists with one WHERE IN.
        var doctorIds = snapshot.LatestPending.Select(r => r.DoctorId)
            .Concat(snapshot.LatestFragmented.Select(r => r.DoctorId))
            .Distinct()
            .ToList();
        var doctorNames = await _staff.GetDoctorNamesAsync(doctorIds, cancellationToken);

        return Result<DashboardSummaryDto>.Success(snapshot.ToDto(
            Map(snapshot.LatestPending, doctorNames),
            Map(snapshot.LatestFragmented, doctorNames),
            now));
    }

    /// <summary>
    /// Only runs when the prescriptions table is empty (fresh/wiped database).
    /// Every prescription-derived number is 0 by construction and dispensing is
    /// 0 (records cannot exist without a prescription FK), so only stock/expiry
    /// are computed — anchored on medicines. If that table is empty too, all
    /// counters are genuinely 0: no medicines means no variants (low-stock 0)
    /// and no batches (FK chain), hence expiring-soon 0.
    /// </summary>
    private async Task<DashboardSummarySnapshot> PrescriptionlessFallbackAsync(CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiringLimit = asOf.AddDays(30);

        var rest = await _executor.SingleOrDefaultAsync(
            from _ in _medicines.Query().Take(1)
            select new
            {
                LowStock = _medicines.Query()
                    .Where(m => m.IsActive)
                    .SelectMany(m => m.Variants
                        .Where(v => v.IsActive && !v.IsDeleted)
                        .Select(v => new
                        {
                            Available = v.Batches
                                .Where(b => !b.IsDeleted && b.ExpiryDate > asOf)
                                .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                            ReorderLevel = v.ReorderLevel.Value
                        }))
                    .Count(x => x.Available <= x.ReorderLevel),
                ExpiringSoon = _medicines.QueryBatches()
                    .Count(b => b.ExpiryDate > asOf && b.ExpiryDate <= expiringLimit)
            },
            cancellationToken);

        return new DashboardSummarySnapshot(0, 0, 0, rest?.LowStock ?? 0, rest?.ExpiringSoon ?? 0, 0, [], []);
    }

    private static IReadOnlyList<PrescriptionListItemDto> Map(
        IEnumerable<DashboardPrescriptionRow> rows,
        IReadOnlyDictionary<Guid, string> doctorNames)
        => rows.Select(r => r.ToListItemDto(doctorNames.GetValueOrDefault(r.DoctorId, string.Empty)))
        .ToList();
}
