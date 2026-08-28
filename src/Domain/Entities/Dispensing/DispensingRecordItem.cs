using Domain.Common;
using Domain.Entities.Medicines;
using Domain.ValueObjects;

namespace Domain.Entities.Dispensing;

public class DispensingRecordItem : BaseEntity
{
    public Guid DispensingRecordId { get; private set; }
    public Guid PrescriptionItemId { get; private set; }
    public Guid MedicineBatchId { get; private set; }
    public MedicineBatch? MedicineBatch { get; private set; }
    public Quantity Quantity { get; private set; } = Quantity.Zero;

    private DispensingRecordItem() { }

    internal DispensingRecordItem(Guid dispensingRecordId, Guid prescriptionItemId, Guid medicineBatchId, int quantity)
    {
        if (medicineBatchId == Guid.Empty)
            throw new ArgumentException("MedicineBatchId is required.", nameof(medicineBatchId));
        if (prescriptionItemId == Guid.Empty)
            throw new ArgumentException("PrescriptionItemId is required.", nameof(prescriptionItemId));

        var qty = Quantity.Of(quantity);
        if (qty.IsZero)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        DispensingRecordId = dispensingRecordId;
        PrescriptionItemId = prescriptionItemId;
        MedicineBatchId = medicineBatchId;
        Quantity = qty;
    }
}