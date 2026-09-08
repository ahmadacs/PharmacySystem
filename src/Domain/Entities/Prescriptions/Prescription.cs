using Domain.Common;
using Domain.Entities.Patients;
using Domain.Entities.Staff;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;

namespace Domain.Entities.Prescriptions;

public class Prescription : BaseEntity
{
    public Guid DoctorId { get; private set; }
    public Doctor? Doctor { get; private set; }

    public Guid PatientId { get; private set; }
    public Patient? Patient { get; private set; }
    public string? Diagnosis { get; private set; }
    public DateOnly IssuedDate { get; private set; }
    public PrescriptionStatus Status { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    private readonly List<PrescriptionItem> _items = new();
    public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();

    private Prescription() { }

    public Prescription(Guid doctorId, Guid patientId, DateOnly issuedDate,
        string? diagnosis = null)
    {
        if (doctorId == Guid.Empty)
            throw new ArgumentException("DoctorId is required.", nameof(doctorId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId is required.", nameof(patientId));

        DoctorId = doctorId;
        PatientId = patientId;
        Diagnosis = diagnosis?.Trim();
        IssuedDate = issuedDate;
        Status = PrescriptionStatus.Pending;

        RaiseDomainEvent(new PrescriptionCreatedEvent(Id, DateTime.UtcNow));
    }

    public void AddItem(
        Guid medicineVariantId,
        int prescribedQuantity,
        string? dosageInstructions = null,
        bool isRefillable = false,
        int refillsAllowed = 0,
        int refillIntervalDays = 0)
    {
        if (Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Expired)
            throw new InvalidPrescriptionStatusException($"Cannot add items to a prescription in '{Status}' status.");

        _items.Add(new PrescriptionItem(Id, medicineVariantId, prescribedQuantity, dosageInstructions, isRefillable, refillsAllowed, refillIntervalDays));
    }

    public void Cancel()
    {
        if (Status == PrescriptionStatus.Cancelled)
            throw new InvalidPrescriptionStatusException("The prescription is already cancelled.");
        if (Status == PrescriptionStatus.FullyDispensed)
            throw new InvalidPrescriptionStatusException("A fully dispensed prescription cannot be cancelled.");

        Status = PrescriptionStatus.Cancelled;
        RaiseDomainEvent(new PrescriptionCancelledEvent(Id, DateTime.UtcNow));
    }

    public void MarkExpired()
    {
        if (Status is PrescriptionStatus.FullyDispensed or PrescriptionStatus.Cancelled)
            return;

        Status = PrescriptionStatus.Expired;
    }

    /// <summary>Validates the prescription is in a state that allows dispensing at all.</summary>
    public void EnsureCanBeDispensed(DateOnly asOf)
    {
        if (Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Expired or PrescriptionStatus.FullyDispensed)
            throw new InvalidPrescriptionStatusException($"Prescription '{Id}' cannot be dispensed while in '{Status}' status.");

        if (Items.Count == 0)
            throw new InvalidPrescriptionStatusException($"Prescription '{Id}' has no items to dispense.");
    }

    public void ApplyDispensedQuantities(
        IReadOnlyDictionary<Guid, int> quantitiesByPrescriptionItemId,
        DateOnly dispensedOn)
    {
        foreach (var (itemId, quantity) in quantitiesByPrescriptionItemId)
        {
            var item = _items.SingleOrDefault(i => i.Id == itemId)
                ?? throw new InvalidPrescriptionStatusException(
                    $"Prescription item '{itemId}' does not belong to prescription '{Id}'.");

            item.RecordDispensed(quantity);
            item.SetLastDispensedAt(dispensedOn);
        }

        Status = _items.All(i => i.IsFullyDispensed)
            ? PrescriptionStatus.FullyDispensed
            : PrescriptionStatus.PartiallyDispensed;

        RaiseDomainEvent(new PrescriptionDispensedEvent(
            Id,
            DateTime.UtcNow,
            quantitiesByPrescriptionItemId.Sum(kv => kv.Value)));
    }

    /// <summary>
    /// Refills a single item. Only that item must be fully dispensed; the rest
    /// of the prescription may be in any dispensed state. Cancelled/expired
    /// prescriptions can never be refilled.
    /// </summary>
    public void RegisterItemRefill(Guid prescriptionItemId)
        => RegisterItemsRefill([prescriptionItemId]);

    /// <summary>
    /// Refills several items atomically: every id is validated first so a
    /// partially-eligible batch never applies half a refill.
    /// </summary>
    public void RegisterItemsRefill(IReadOnlyCollection<Guid> prescriptionItemIds)
    {
        if (Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Expired)
            throw new InvalidPrescriptionStatusException($"Prescription '{Id}' cannot be refilled while in '{Status}' status.");

        if (prescriptionItemIds.Count == 0)
            throw new ArgumentException("At least one prescription item is required.", nameof(prescriptionItemIds));

        var distinctIds = prescriptionItemIds.Distinct().ToList();
        var targets = new List<PrescriptionItem>(distinctIds.Count);
        foreach (var itemId in distinctIds)
        {
            var item = _items.SingleOrDefault(i => i.Id == itemId)
                ?? throw new InvalidPrescriptionStatusException(
                    $"Prescription item '{itemId}' does not belong to prescription '{Id}'.");
            // Validate all before mutating any (atomic batch semantics).
            item.EnsureEligibleForRefill();
            targets.Add(item);
        }

        foreach (var item in targets)
            item.RegisterRefill();

        RefreshStatusAfterRefill();
        RaiseDomainEvent(new PrescriptionRefilledEvent(Id, distinctIds.AsReadOnly(), DateTime.UtcNow));
    }

    private void RefreshStatusAfterRefill()
    {
        if (_items.All(i => i.IsFullyDispensed))
        {
            Status = PrescriptionStatus.FullyDispensed;
            return;
        }

        Status = _items.Any(i => i.DispensedQuantity.Value > 0)
            ? PrescriptionStatus.PartiallyDispensed
            : PrescriptionStatus.Pending;
    }
}