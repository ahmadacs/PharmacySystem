using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Application.Features.Inventory.Queries;
using Application.Features.Medicines.Dtos;
using Application.Features.Medicines.Queries;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IMedicineRepository : IBaseRepository<Medicine>
{
    Task<MedicineBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Medicine?> GetByIdWithVariantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicineVariant?> GetVariantByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the scientific name with the given name, creating and tracking a
    /// new one when it does not exist yet. Returns null when the name is blank.
    /// </summary>
    Task<GenericName> GetOrCreateGenericNameAsync(string name, string? nameAr = null, CancellationToken cancellationToken = default);

    /// <summary>Returns a variant with the given medicine + form + unit + strength, or null.</summary>
    Task<MedicineVariant?> FindVariantAsync(Guid medicineId, MedicineForm form, MedicineUnit unit, decimal strength, CancellationToken cancellationToken = default);

    /// <summary>Loads the variants with the given ids, used to validate prescription items in one round trip.</summary>
    Task<List<MedicineVariant>> GetVariantsByIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default);

    /// <summary>Loads variants with their batches, used by the atomic dispensing flow.</summary>
    Task<List<MedicineVariant>> GetForDispensingAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default);

    /// <summary>Loads tracked medicine aggregates (variants + batches) for the given
    /// variant ids, so stock/expiry domain events can be raised after a stock change.</summary>
    Task<List<Medicine>> GetMedicinesByVariantIdsForStockCheckAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default);

    Task<PagedList<MedicineListItemDto>> ListAsync(ListMedicinesQuery query, CancellationToken cancellationToken = default);
    Task<PagedList<MedicineBatchDto>> ListBatchesAsync(BatchListQuery query, CancellationToken cancellationToken = default);
    Task<PagedList<MedicineInventorySummaryDto>> ListMedicineSummaryAsync(MedicineInventorySummaryQuery query, CancellationToken cancellationToken = default);
    Task<PagedList<ExpiryAlertDto>> ListExpiryAlertsAsync(ExpiryAlertListQuery query, CancellationToken cancellationToken = default);
    Task<PagedList<InventoryAdjustmentDto>> ListAdjustmentsAsync(InventoryAdjustmentListQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockDto>> GetLowStockAsync(CancellationToken cancellationToken = default);

    Task<bool> MedicineNameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> BatchNumberExistsAsync(string batchNumber, Guid? excludeId, CancellationToken cancellationToken = default);

    void AddBatch(MedicineBatch batch);
    void RemoveBatch(MedicineBatch batch);
    void AddVariant(MedicineVariant variant);
    void RemoveVariant(MedicineVariant variant);
    void AddAdjustment(InventoryAdjustment adjustment);
}