using System.Globalization;
using Domain.Common;
using Domain.Entities.Dispensing;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Medicines;

public class MedicineBatch : BaseAggregateRoot
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

    private readonly List<DispensingRecordItem> _dispensingItems = new();
    public IReadOnlyCollection<DispensingRecordItem> DispensingItems => _dispensingItems.AsReadOnly();

    private MedicineBatch() { }

    public MedicineBatch(Guid medicineVariantId, string batchNumber, DateOnly manufactureDate, DateOnly expiryDate,
        int packagesReceived, UnitOfMeasure unitOfMeasure, decimal unitCost, string? supplierName = null)
    {
        if (medicineVariantId == Guid.Empty)
            throw new ArgumentException("MedicineVariantId is required.", nameof(medicineVariantId));
        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("Batch number is required.", nameof(batchNumber));
        if (expiryDate <= manufactureDate)
            throw new InvalidBatchDatesException(manufactureDate, expiryDate);
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

    public static string GenerateNumber(
        string medicineName,
        MedicineForm form,
        MedicineUnit unit,
        decimal strength,
        DateOnly today)
    {
        var namePart = new string(medicineName.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(namePart))
            namePart = "MED";

        var formPart = form.ToString()[0].ToString().ToUpperInvariant();
        var unitPart = unit.ToString()[0].ToString().ToUpperInvariant();
        var strengthPart = strength.ToString("0.##", CultureInfo.InvariantCulture).Replace(".", "");

        var variantPart = $"{formPart}{unitPart}{strengthPart}";
        if (string.IsNullOrWhiteSpace(variantPart))
            variantPart = "VAR";

        return $"{namePart}-{variantPart}-{today:yyMMdd}";
    }

    public bool HasSufficientStock(int quantity) => QuantityAvailable.Value >= quantity;

    public void ReduceStock(int quantity, DateOnly asOf)
    {
        var toReduce = Quantity.Of(quantity);
        if (toReduce.IsZero)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to dispense must be positive.");
        if (IsExpired(asOf))
            throw new ExpiredBatchException(Id, ExpiryDate, BatchNumber);
        if (!HasSufficientStock(toReduce.Value))
            throw new InsufficientStockException(Id, toReduce.Value, QuantityAvailable.Value);

        QuantityAvailable = QuantityAvailable.Subtract(toReduce);
    }

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