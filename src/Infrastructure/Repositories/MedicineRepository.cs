using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MedicineRepository : BaseRepository<Medicine>, IMedicineRepository
{
    public MedicineRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<GenericName?> FindGenericNameAsync(string name, CancellationToken cancellationToken = default)
        => await Db.Set<GenericName>().FirstOrDefaultAsync(g => g.Name == name, cancellationToken);

    public void AddGenericName(GenericName genericName)
        // Add only to the DbContext. The Unit of Work / caller is responsible for SaveChanges.
        => Db.Set<GenericName>().Add(genericName);

    public async Task<Medicine?> GetByIdWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<Medicine>()
            .AsNoTracking()
            .Include(m => m.Variants)
                .ThenInclude(v => v.Batches)
            .Include(m => m.GenericName)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<MedicineVariant?> GetVariantByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<MedicineVariant?> FindVariantAsync(Guid medicineId, MedicineForm form, MedicineUnit unit, decimal strength, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.MedicineId == medicineId && v.Form == form && v.Unit == unit && v.Strength == strength, cancellationToken);

    public async Task<List<MedicineVariant>> GetVariantsByIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>()
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

    public async Task<List<MedicineVariant>> GetForDispensingAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>()
            .Include(v => v.Batches)
            .Include(v => v.Medicine)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

    public void AddVariant(MedicineVariant variant)
        => Db.Set<MedicineVariant>().Add(variant);

    public void RemoveVariant(MedicineVariant variant)
        => Db.Set<MedicineVariant>().Remove(variant);

    public async Task<MedicineBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineBatch>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public void AddBatch(MedicineBatch batch)
        => Db.Set<MedicineBatch>().Add(batch);

    public void RemoveBatch(MedicineBatch batch)
        => Db.Set<MedicineBatch>().Remove(batch);

    public void AddAdjustment(InventoryAdjustment adjustment)
        => Db.Set<InventoryAdjustment>().Add(adjustment);

    public async Task<bool> MedicineNameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        return await Db.Set<Medicine>()
            .AnyAsync(m => m.Name == trimmed && (excludeId == null || m.Id != excludeId), cancellationToken);
    }

    public async Task<bool> BatchNumberExistsAsync(string batchNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var trimmed = batchNumber.Trim();
        return await Db.Set<MedicineBatch>()
            .AnyAsync(b => b.BatchNumber == trimmed && (excludeId == null || b.Id != excludeId), cancellationToken);
    }

    /// <summary>Queryable batch set for handler-built searches. Pure data access.</summary>
    public IQueryable<MedicineBatch> QueryBatches()
        => Db.Set<MedicineBatch>();

    /// <summary>Queryable adjustment set for handler-built searches. Pure data access.</summary>
    public IQueryable<InventoryAdjustment> QueryAdjustments()
        => Db.Set<InventoryAdjustment>();
}