using Domain.Common;
using Domain.Entities.Prescriptions;
using Domain.Entities.Staff;

namespace Domain.Entities.Dispensing;

public class DispensingRecord : BaseEntity
{
    public Guid PrescriptionId { get; private set; }
    public Prescription? Prescription { get; private set; }

    public Guid PharmacistId { get; private set; }
    public Pharmacist? Pharmacist { get; private set; }

    public DateTime DispensedAt { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<DispensingRecordItem> _items = new();
    public IReadOnlyCollection<DispensingRecordItem> Items => _items.AsReadOnly();

    private DispensingRecord() { }

    public DispensingRecord(Guid prescriptionId, Guid pharmacistId, DateTime dispensedAt, string? notes = null)
    {
        if (prescriptionId == Guid.Empty)
            throw new ArgumentException("PrescriptionId is required.", nameof(prescriptionId));
        if (pharmacistId == Guid.Empty)
            throw new ArgumentException("PharmacistId is required.", nameof(pharmacistId));

        PrescriptionId = prescriptionId;
        PharmacistId = pharmacistId;
        DispensedAt = dispensedAt;
        Notes = notes?.Trim();
    }

    public void AddLine(Guid prescriptionItemId, Guid medicineBatchId, int quantity)
    {
        _items.Add(new DispensingRecordItem(Id, prescriptionItemId, medicineBatchId, quantity));
    }

    public void SetNotes(string? notes) => Notes = notes?.Trim();

    public IReadOnlyDictionary<Guid, int> GetQuantitiesByPrescriptionItem() =>
        _items
            .GroupBy(i => i.PrescriptionItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity.Value));
}