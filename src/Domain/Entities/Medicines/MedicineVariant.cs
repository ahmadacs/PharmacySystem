using Domain.Common;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Medicines;

public class MedicineVariant : BaseEntity
{
    public Guid MedicineId { get; private set; }
    public Medicine? Medicine { get; private set; }

    public MedicineForm Form { get; private set; }
    public MedicineUnit Unit { get; private set; }
    public decimal Strength { get; private set; }
    public Quantity ReorderLevel { get; private set; } = Quantity.Zero;
    public UnitOfMeasure UnitOfMeasure { get; private set; } = UnitOfMeasure.Create("Unit", "Box", 1);
    public bool IsActive { get; private set; } = true;

    private readonly List<MedicineBatch> _batches = new();
    public IReadOnlyCollection<MedicineBatch> Batches => _batches.AsReadOnly();

    private MedicineVariant() { }

    public MedicineVariant(Guid medicineId, MedicineForm form, MedicineUnit unit, decimal strength,
        int reorderLevel = 10, UnitOfMeasure? unitOfMeasure = null)
    {
        if (medicineId == Guid.Empty)
            throw new ArgumentException("MedicineId is required.", nameof(medicineId));
        if (strength <= 0)
            throw new ArgumentException("Strength must be greater than zero.", nameof(strength));

        MedicineId = medicineId;
        Form = form;
        Unit = unit;
        Strength = strength;
        ReorderLevel = Quantity.Of(reorderLevel);
        UnitOfMeasure = unitOfMeasure ?? UnitOfMeasure.Create("Unit", "Box", 1);
    }

    public void UpdateDetails(MedicineForm form, MedicineUnit unit, decimal strength, int reorderLevel, UnitOfMeasure? unitOfMeasure = null)
    {
        if (strength <= 0)
            throw new ArgumentException("Strength must be greater than zero.", nameof(strength));

        Form = form;
        Unit = unit;
        Strength = strength;
        ReorderLevel = Quantity.Of(reorderLevel);
        if (unitOfMeasure is not null)
            UnitOfMeasure = unitOfMeasure;
    }

    public void UpdateReorderLevel(int reorderLevel) => ReorderLevel = Quantity.Of(reorderLevel);

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public bool IsLowStock(DateOnly asOf) => GetAvailableStock(asOf).Value <= ReorderLevel.Value;

    /// <summary>
    /// Raises MedicineLowStockEvent when this variant's available stock is at or below its reorder level.
    /// </summary>
    public void RaiseLowStockEventIfNeeded(DateOnly asOf)
    {
        if (!IsLowStock(asOf))
            return;

        var variantName = $"{Form} {Strength} {Unit}";
        var medicineName = Medicine?.Name ?? "Unknown";
        RaiseDomainEvent(new MedicineLowStockEvent(
            MedicineId,
            Id,
            medicineName,
            variantName,
            GetAvailableStock(asOf).Value,
            ReorderLevel.Value,
            DateTime.UtcNow));
    }

    /// <summary>
    /// Raises low-stock considering an additional quantity that will be added (e.g., a new batch not yet in Batches collection).
    /// </summary>
    public void RaiseLowStockEventIfNeededWithAdditional(DateOnly asOf, int additionalQuantity)
    {
        var availableAfter = GetAvailableStock(asOf).Value + additionalQuantity;
        if (availableAfter > ReorderLevel.Value)
            return;
        var variantName = $"{Form} {Strength} {Unit}";
        var medicineName = Medicine?.Name ?? "Unknown";
        RaiseDomainEvent(new MedicineLowStockEvent(
            MedicineId,
            Id,
            medicineName,
            variantName,
            availableAfter,
            ReorderLevel.Value,
            DateTime.UtcNow));
    }

    public void AddBatch(MedicineBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        _batches.Add(batch);
    }

    public Quantity GetAvailableStock(DateOnly asOf) =>
        _batches
            .NotDeleted()
            .Where(b => !b.IsExpired(asOf))
            .Aggregate(Quantity.Zero, (total, b) => total.Add(b.QuantityAvailable));

    public IEnumerable<MedicineBatch> GetExpiredBatches(DateOnly asOf) =>
        _batches.NotDeleted().Where(b => b.IsExpired(asOf));

    public IEnumerable<MedicineBatch> GetNearExpiryBatches(DateOnly asOf, int withinDays) =>
        _batches.NotDeleted()
            .Where(b => !b.IsExpired(asOf)
                         && b.ExpiryDate.DayNumber - asOf.DayNumber <= withinDays);

    /// <summary>
    /// Draws the required quantity from non-expired batches (earliest expiry first).
    /// Throws <see cref="InsufficientStockException"/> when the variant cannot cover it.
    /// </summary>
    public IReadOnlyList<(MedicineBatch Batch, Quantity Quantity)> SelectBatchesForDispensing(int quantityNeeded, DateOnly asOf)
    {
        var needed = Quantity.Of(quantityNeeded);
        var plan = new List<(MedicineBatch Batch, Quantity Quantity)>();
        var remaining = needed.Value;

        var eligibleBatches = _batches
            .NotDeleted()
            .Where(b => !b.IsExpired(asOf) && b.QuantityAvailable.Value > 0)
            .OrderBy(b => b.ExpiryDate);

        foreach (var batch in eligibleBatches)
        {
            if (remaining <= 0) break;

            var take = Math.Min(remaining, batch.QuantityAvailable.Value);
            plan.Add((batch, Quantity.Of(take)));
            remaining -= take;
        }

        var fulfilled = needed.Value - remaining;

        // If nothing could be fulfilled, keep throwing as before.
        if (fulfilled == 0)
            throw new InsufficientStockException(Id, needed.Value, needed.Value - remaining);

        // Otherwise return the plan (possibly partial) so caller can record partial dispenses.
        return plan;
    }
}