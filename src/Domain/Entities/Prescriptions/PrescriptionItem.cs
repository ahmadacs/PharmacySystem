using Domain.Common;
using Domain.Entities.Medicines;
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

    private PrescriptionItem() { }

    internal PrescriptionItem(Guid prescriptionId, Guid medicineVariantId, int prescribedQuantity, string? dosageInstructions)
    {
        if (medicineVariantId == Guid.Empty)
            throw new ArgumentException("MedicineVariantId is required.", nameof(medicineVariantId));

        var quantity = Quantity.Of(prescribedQuantity);
        if (quantity.IsZero)
            throw new ArgumentOutOfRangeException(nameof(prescribedQuantity), "Prescribed quantity must be positive.");

        PrescriptionId = prescriptionId;
        MedicineVariantId = medicineVariantId;
        PrescribedQuantity = quantity;
        DosageInstructions = dosageInstructions?.Trim();
        DispensedQuantity = Quantity.Zero;
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

    internal void ResetForRefill() => DispensedQuantity = Quantity.Zero;
}