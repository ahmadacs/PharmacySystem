using Domain.Common;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Prescriptions;

public class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; private set; }
    public Guid MedicineVariantId { get; private set; }
    public MedicineVariant? MedicineVariant { get; private set; }

    public Quantity PrescribedQuantity { get; private set; } = Quantity.Zero;
    public Quantity DispensedQuantity { get; private set; } = Quantity.Zero;
    public string? DosageInstructions { get; private set; }

    public bool IsRefillable { get; private set; }
    public int RefillsAllowed { get; private set; }
    public int RefillsUsed { get; private set; }

    /// <summary>Minimum days between two dispenses of this item. 0 = no constraint.</summary>
    public int RefillIntervalDays { get; private set; }

    /// <summary>Date of the last successful dispense. Null = never dispensed.</summary>
    public DateOnly? LastDispensedAt { get; private set; }

    private PrescriptionItem() { }

    internal PrescriptionItem(
        Guid prescriptionId,
        Guid medicineVariantId,
        int prescribedQuantity,
        string? dosageInstructions,
        bool isRefillable = false,
        int refillsAllowed = 0,
        int refillIntervalDays = 0)
    {
        if (medicineVariantId == Guid.Empty)
            throw new ArgumentException("MedicineVariantId is required.", nameof(medicineVariantId));

        var quantity = Quantity.Of(prescribedQuantity);
        if (quantity.IsZero)
            throw new ArgumentOutOfRangeException(nameof(prescribedQuantity), "Prescribed quantity must be positive.");

        if (isRefillable && refillsAllowed <= 0)
            throw new ArgumentException("A refillable prescription item must allow at least one refill.", nameof(refillsAllowed));
        if (!isRefillable && refillsAllowed != 0)
            throw new ArgumentException("A non-refillable prescription item must have zero refills allowed.", nameof(refillsAllowed));
        if (refillIntervalDays < 0)
            throw new ArgumentOutOfRangeException(nameof(refillIntervalDays), "Refill interval cannot be negative.");

        PrescriptionId = prescriptionId;
        MedicineVariantId = medicineVariantId;
        PrescribedQuantity = quantity;
        DosageInstructions = dosageInstructions?.Trim();
        DispensedQuantity = Quantity.Zero;
        IsRefillable = isRefillable;
        RefillsAllowed = refillsAllowed;
        RefillsUsed = 0;
        RefillIntervalDays = refillIntervalDays;
        LastDispensedAt = null;
    }

    public Quantity RemainingQuantity => PrescribedQuantity.Subtract(DispensedQuantity);
    public bool IsFullyDispensed => DispensedQuantity.Value >= PrescribedQuantity.Value;

    internal void RecordDispensed(int quantity)
    {
        var toAdd = Quantity.Of(quantity);
        if (toAdd.IsZero)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Dispensed quantity must be positive.");
        if (toAdd.Value > RemainingQuantity.Value)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Cannot dispense more than the remaining prescribed quantity.");

        DispensedQuantity = DispensedQuantity.Add(toAdd);
    }

    public void EnsureEligibleForRefill()
    {
        if (!IsRefillable)
            throw new RefillNotEligibleException($"Prescription item '{Id}' is not marked as refillable.");
        if (!IsFullyDispensed)
            throw new RefillNotEligibleException($"Prescription item '{Id}' must be fully dispensed before it can be refilled.");
        if (RefillsUsed >= RefillsAllowed)
            throw new RefillNotEligibleException(
                $"Prescription item '{Id}' has no refills remaining ({RefillsUsed}/{RefillsAllowed} used).");
    }

    internal void RegisterRefill()
    {
        EnsureEligibleForRefill();
        RefillsUsed++;
        DispensedQuantity = Quantity.Zero;
    }

    /// <summary>
    /// Time gate for actual dispensing (called from the dispense path only —
    /// refill authorization itself is never time-gated). Skipped when the item
    /// has no interval (0) or was never dispensed.
    /// </summary>
    public void EnsureRefillIntervalSatisfied(DateOnly today)
    {
        if (RefillIntervalDays <= 0 || LastDispensedAt is null)
            return;

        var nextEligible = LastDispensedAt.Value.AddDays(RefillIntervalDays);
        if (today < nextEligible)
            throw new RefillIntervalNotSatisfiedException(nextEligible,
                $"Prescription item '{Id}' cannot be dispensed before {nextEligible:yyyy-MM-dd} (refill interval {RefillIntervalDays} days).");
    }

    internal void SetLastDispensedAt(DateOnly dispensedOn) => LastDispensedAt = dispensedOn;
}