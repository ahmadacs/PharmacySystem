using Application.Common.Interfaces;
using Domain.Entities.Dispensing;
using Domain.Entities.Prescriptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PrescriptionRepository : BaseRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<Prescription?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<Prescription>()
            .Include(p => p.Patient)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Prescription?> GetByIdWithItemsAndDoctorAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<Prescription>()
            .Include(p => p.Patient)
            .Include(p => p.Items)
            .Include(p => p.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<List<Prescription>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
        => await Db.Set<Prescription>()
            .Include(p => p.Patient)
            .Include(p => p.Items)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssuedDate)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, int>> GetDispensedTotalsAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken = default)
        => await Db.Set<DispensingRecordItem>()
            .Where(i => batchIds.Contains(i.MedicineBatchId))
            .GroupBy(i => i.MedicineBatchId)
            .Select(g => new { BatchId = g.Key, Total = g.Sum(i => i.Quantity.Value) })
            .ToDictionaryAsync(x => x.BatchId, x => x.Total, cancellationToken);

    /// <summary>Queryable dispensing-record set for handler-built searches. Pure data access.</summary>
    public IQueryable<DispensingRecord> QueryDispensingRecords()
        => Db.Set<DispensingRecord>();

    /// <summary>Queryable dispensing-line set for handler-built subqueries. Pure data access.</summary>
    public IQueryable<DispensingRecordItem> QueryDispensingRecordItems()
        => Db.Set<DispensingRecordItem>();

    public void AddDispensingRecord(DispensingRecord record)
        => Db.Set<DispensingRecord>().Add(record);
}