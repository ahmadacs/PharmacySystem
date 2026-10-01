using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private static readonly DashboardCounts Empty = new(0, 0, 0, 0, 0, 0, 0, 0);

    private readonly ApplicationDbContext _db;

    public DashboardRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardCounts> GetCountsAsync(
        DateOnly asOf,
        DateOnly expiringLimit,
        DateTime today,
        DateTime tomorrow,
        CancellationToken cancellationToken = default)
    {
        return await _db.Medicines
            .Select(_ => new DashboardCounts(
                TotalMedicines: _db.Medicines.Count(m => m.IsActive),
                TotalVariants: _db.MedicineVariants.Count(v => v.IsActive),
                PrescriptionsCreatedToday: _db.Prescriptions.Count(p => p.IssuedDate == asOf),
                DispensedToday: _db.DispensingRecords.Count(r => r.DispensedAt >= today && r.DispensedAt <= tomorrow),
                AdjustmentsToday: _db.InventoryAdjustments.Count(a => a.AdjustedAt >= today && a.AdjustedAt <= tomorrow),
                LowStock: _db.MedicineVariants.Count(v => v.IsActive && v.Medicine!.IsActive
                    && v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value),
                ExpiredBatches: _db.MedicineBatches.Count(b => b.ExpiryDate <= asOf),
                ExpiringSoon: _db.MedicineBatches.Count(b => b.ExpiryDate > asOf && b.ExpiryDate <= expiringLimit)))
            .FirstOrDefaultAsync(cancellationToken)
            ?? Empty;
    }
}
