using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Common.Specifications;
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
        var dispensedTodaySpec = new Specification<DispensingRecord, DispensingRecord>(r => r);
        dispensedTodaySpec.Where(r => r.DispensedAt >= today && r.DispensedAt <= tomorrow);
        var dispensedToday = await _dispensing.CountAsync(dispensedTodaySpec, cancellationToken);

        var pendingSpec = new Specification<Prescription, Prescription>(p => p);
        pendingSpec.Where(p => p.Status == PrescriptionStatus.Pending);
        var pending = await _prescriptions.CountAsync(pendingSpec, cancellationToken);

        var createdTodaySpec = new Specification<Prescription, Prescription>(p => p);
        createdTodaySpec.Where(p => p.IssuedDate == asOf);
        var createdToday = await _prescriptions.CountAsync(createdTodaySpec, cancellationToken);

        var fragmentedSpec = new Specification<Prescription, Prescription>(p => p);
        fragmentedSpec.Where(p => p.Status == PrescriptionStatus.PartiallyDispensed);
        var fragmented = await _prescriptions.CountAsync(fragmentedSpec, cancellationToken);

        // Same rule as ListLowStockQueryHandler: available non-expired stock at
        // or below the variant reorder level.
        var lowStockSpec = new Specification<MedicineVariant, MedicineVariant>(v => v);
        lowStockSpec.Where(v => v.IsActive && v.Medicine!.IsActive);
        lowStockSpec.Where(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value);
        var lowStock = await _variants.CountAsync(lowStockSpec, cancellationToken);

        // Same window as the near-expiry domain rule (NotificationOptions.ExpiryWarningDays).
        var expiringSpec = new Specification<MedicineBatch, MedicineBatch>(b => b);
        expiringSpec.Where(b => b.ExpiryDate > asOf && b.ExpiryDate <= expiringLimit);
        var expiringSoon = await _batches.CountAsync(expiringSpec, cancellationToken);

        // Doctor names stay out: Identity tables are invisible to this layer,
        // so names resolve with ONE batched lookup below (no N+1).
        var latestPending = await _prescriptions.ListAsync(LatestSpec(PrescriptionStatus.Pending), cancellationToken);
        var latestFragmented = await _prescriptions.ListAsync(LatestSpec(PrescriptionStatus.PartiallyDispensed), cancellationToken);

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

    private static Specification<Prescription, DashboardPrescriptionRow> LatestSpec(PrescriptionStatus status)
    {
        var spec = new Specification<Prescription, DashboardPrescriptionRow>(p => new DashboardPrescriptionRow(
            p.Id,
            p.DoctorId,
            p.Patient == null ? string.Empty : (p.Patient.FirstName + " " + p.Patient.LastName).Trim(),
            p.Patient != null ? p.Patient.DateOfBirth : default,
            p.Patient != null ? p.Patient.PhoneNumber : null,
            p.IssuedDate,
            p.Status,
            p.Items.Count()));
        spec.Where(p => p.Status == status);
        spec.Order(q => q.OrderByDescending(p => p.IssuedDate));
        spec.Page(0, LatestTake);
        return spec;
    }

    private static IReadOnlyList<PrescriptionListItemDto> Map(
        IEnumerable<DashboardPrescriptionRow> rows,
        IReadOnlyDictionary<Guid, string> doctorNames)
        => rows.Select(r => r.ToListItemDto(doctorNames.GetValueOrDefault(r.DoctorId, string.Empty)))
        .ToList();
}
