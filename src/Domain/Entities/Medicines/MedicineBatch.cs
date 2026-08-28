using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Medicines;

public class MedicineBatch : BaseEntity
{
    public Guid MedicineVariantId { get; private set; }
    public MedicineVariant? MedicineVariant { get; private set; }

    public string BatchNumber { get; private set; } = string.Empty;
    public DateOnly ManufactureDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public Quantity QuantityReceived { get; private set; } = Quantity.Zero;
    public Quantity QuantityAvailable { get; private set; } = Quantity.Zero;
    public Money UnitCost { get; private set; } = Money.Zero;
    public string? SupplierName { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    private MedicineBatch() { }

    /// <summary>
    /// Creates a batch from a whole-package count. The package count is converted
    /// to base units using the variant's <see cref="UnitOfMeasure"/> (e.g. 5 boxes
    /// of 30 tablets => 150 tablets), so the stored quantity is always a multiple
    /// of <c>UnitsPerPackage</c>. Dispensing then happens in base units.
    /// </summary>
    public MedicineBatch(Guid medicineVariantId, string batchNumber, DateOnly manufactureDate, DateOnly expiryDate,
        int packagesReceived, UnitOfMeasure unitOfMeasure, decimal unitCost, string? supplierName = null)
    {
        if (medicineVariantId == Guid.Empty)
            throw new ArgumentException("MedicineVariantId is required.", nameof(medicineVariantId));
        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("Batch number is required.", nameof(batchNumber));
        if (expiryDate <= manufactureDate)
            throw new ArgumentException("Expiry date must be after the manufacture date.", nameof(expiryDate));
        ArgumentNullException.ThrowIfNull(unitOfMeasure);

        var quantity = unitOfMeasure.PackagesToBaseUnits(packagesReceived);
        if (quantity.IsZero)
            throw new ArgumentOutOfRangeException(nameof(packagesReceived), "Quantity received must be positive.");

        MedicineVariantId = medicineVariantId;
        BatchNumber = batchNumber.Trim();
        ManufactureDate = manufactureDate;
        ExpiryDate = expiryDate;
        QuantityReceived = quantity;
        QuantityAvailable = quantity;
        UnitCost = Money.Of(unitCost);
        SupplierName = supplierName?.Trim();
    }

    public bool IsExpired(DateOnly asOf) => ExpiryDate <= asOf;

    public bool HasSufficientStock(int quantity) => QuantityAvailable.Value >= quantity;

    public void ReduceStock(int quantity, DateOnly asOf)
    {
        var toReduce = Quantity.Of(quantity);
        if (toReduce.IsZero)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to dispense must be positive.");
        if (IsExpired(asOf))
            throw new ExpiredBatchException(Id, ExpiryDate);
        if (!HasSufficientStock(toReduce.Value))
            throw new InsufficientStockException(Id, toReduce.Value, QuantityAvailable.Value);

        QuantityAvailable = QuantityAvailable.Subtract(toReduce);
    }

    /// <summary>
    /// Raises MedicineBatchNearExpiryEvent when the batch expires within
    /// <paramref name="withinDays"/> days and is not already expired.
    /// </summary>
    public void RaiseNearExpiryEventIfNeeded(DateOnly asOf, int withinDays)
    {
        if (IsExpired(asOf))
            return;
        if (ExpiryDate.DayNumber - asOf.DayNumber > withinDays)
            return;

        RaiseDomainEvent(new MedicineBatchNearExpiryEvent(
            Id, MedicineVariantId, BatchNumber, ExpiryDate, DateTime.UtcNow));
    }

    public void AdjustQuantity(int delta)
    {
        if (delta == 0)
            throw new ArgumentException("Adjustment delta cannot be zero.", nameof(delta));

        var newValue = QuantityAvailable.Value + delta;
        if (newValue < 0)
            throw new InsufficientStockException(Id, -delta, QuantityAvailable.Value);

        QuantityAvailable = Quantity.Of(newValue);
    }
}