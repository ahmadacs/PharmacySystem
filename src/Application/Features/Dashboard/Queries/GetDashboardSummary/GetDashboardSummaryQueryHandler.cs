using System.Linq.Expressions;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Dashboard.Dtos;
using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Dispensing;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    /// <summary>Presentation decision owned by the Application layer.</summary>
    private const int LatestTake = 5;

    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IBaseRepository<DispensingRecord> _dispensing;
    private readonly IBaseRepository<MedicineVariant> _variants;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IStaffService _staff;
    private readonly NotificationOptions _notificationOptions;

    public GetDashboardSummaryQueryHandler(
        IBaseRepository<Prescription> prescriptions,
        IBaseRepository<DispensingRecord> dispensing,
        IBaseRepository<MedicineVariant> variants,
        IBaseRepository<MedicineBatch> batches,
        IStaffService staff,
        NotificationOptions notificationOptions)
    {
        _prescriptions = prescriptions;
        _dispensing = dispensing;
        _variants = variants;
        _batches = batches;
        _staff = staff;
        _notificationOptions = notificationOptions;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var asOf = DateOnly.FromDateTime(now);
        var expiringLimit = asOf.AddDays(_notificationOptions.ExpiryWarningDays);

        // NOTE: no IsDeleted guard anywhere below — the EF global query filter
        // (ApplicationDbContext.ApplySoftDeleteFilters) already excludes
        // soft-deleted rows from every database query.
        //
        // Pure specs: every counter and list below is an independent spec query
        // (no bespoke repository method, no DTO built inside any repo).
        // Sequential awaits are deliberate — one scoped DbContext is shared and
        // EF Core does not allow concurrent operations on it, so Task.WhenAll
        // over specs would throw. Each query is a tiny indexed COUNT/TOP read.
        // An empty database needs no fallback branch: every count below is
        // naturally 0 and both lists naturally empty.

        // Same predicate as DispensingRecordListQueryHandler (inclusive range).
        var dispensedToday = await _dispensing.CountAsync(r => r.DispensedAt >= today && r.DispensedAt <= tomorrow, cancellationToken);

        var pending = await _prescriptions.CountAsync(p => p.Status == PrescriptionStatus.Pending, cancellationToken);

        var createdToday = await _prescriptions.CountAsync(p => p.IssuedDate == asOf, cancellationToken);

        var fragmented = await _prescriptions.CountAsync(p => p.Status == PrescriptionStatus.PartiallyDispensed, cancellationToken);

        // Same rule as ListLowStockQueryHandler: available non-expired stock at
        // or below the variant reorder level.
        var lowStock = await _variants.CountAsync(
            v => v.IsActive && v.Medicine!.IsActive
                && v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value,
            cancellationToken);

        // Same window as the near-expiry domain rule (NotificationOptions.ExpiryWarningDays).
        var expiringSoon = await _batches.CountAsync(b => b.ExpiryDate > asOf && b.ExpiryDate <= expiringLimit, cancellationToken);

        // Doctor names stay out: Identity tables are invisible to this layer,
        // so names resolve with ONE batched lookup below (no N+1).
        // Lean selector: patient name/info are scalar columns; age/status
        // display strings are derived in DashboardMapping (not SQL).
        var latestSelector = (Expression<Func<Prescription, DashboardPrescriptionRow>>)(p => new DashboardPrescriptionRow(
            p.Id,
            p.DoctorId,
            p.Patient == null ? string.Empty : (p.Patient.FirstName + " " + p.Patient.LastName),
            p.Patient != null ? p.Patient.DateOfBirth : default,
            p.Patient != null ? p.Patient.PhoneNumber : null,
            p.IssuedDate,
            p.Status,
            p.Items.Count()));

        var latestPending = await _prescriptions.PagedAsync(
            latestSelector, p => p.Status == PrescriptionStatus.Pending,
            p => p.IssuedDate, true, 1, LatestTake, cancellationToken);
        var latestFragmented = await _prescriptions.PagedAsync(
            latestSelector, p => p.Status == PrescriptionStatus.PartiallyDispensed,
            p => p.IssuedDate, true, 1, LatestTake, cancellationToken);

        var snapshot = new DashboardSummarySnapshot(
            dispensedToday, pending, createdToday, lowStock, expiringSoon, fragmented,
            latestPending, latestFragmented);

        // Every distinct doctor of both lists with one WHERE IN.
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



    private static IReadOnlyList<PrescriptionListItemDto> Map(
        IEnumerable<DashboardPrescriptionRow> rows,
        IReadOnlyDictionary<Guid, string> doctorNames)
        => rows.Select(r => r.ToListItemDto(doctorNames.GetValueOrDefault(r.DoctorId, string.Empty)))
        .ToList();
}
