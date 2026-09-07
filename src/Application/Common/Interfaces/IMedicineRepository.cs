using Application.Common.Models;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IMedicineRepository : IBaseRepository<Medicine>
{
    Task<MedicineBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Medicine?> GetByIdWithVariantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicineVariant?> GetVariantByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the tracked scientific name with the given exact name, or null.</summary>
    Task<GenericName?> FindGenericNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Returns a variant with the given medicine + form + unit + strength, or null.</summary>
    Task<MedicineVariant?> FindVariantAsync(Guid medicineId, MedicineForm form, MedicineUnit unit, decimal strength, CancellationToken cancellationToken = default);

    /// <summary>Loads the variants with the given ids, used to validate prescription items in one round trip.</summary>
    Task<List<MedicineVariant>> GetVariantsByIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default);

    /// <summary>Loads variants with their batches, used by the atomic dispensing flow.</summary>
    Task<List<MedicineVariant>> GetForDispensingAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default);

    /// <summary>Queryable batch set for handler-built searches. Pure data access.</summary>
    IQueryable<MedicineBatch> QueryBatches();

    /// <summary>Queryable adjustment set for handler-built searches. Pure data access.</summary>
    IQueryable<InventoryAdjustment> QueryAdjustments();

    Task<bool> MedicineNameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> BatchNumberExistsAsync(string batchNumber, Guid? excludeId, CancellationToken cancellationToken = default);

    void AddBatch(MedicineBatch batch);
    void RemoveBatch(MedicineBatch batch);
    void AddVariant(MedicineVariant variant);
    void RemoveVariant(MedicineVariant variant);
    void AddAdjustment(InventoryAdjustment adjustment);
    void AddGenericName(GenericName genericName);
}
