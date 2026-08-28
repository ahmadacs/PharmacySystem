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

    public bool IsRefillable { get; private set; }
    public int RefillsAllowed { get; private set; }
    public int RefillsUsed { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    private readonly List<PrescriptionItem> _items = new();
    public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();

    private Prescription() { }

    public Prescription(Guid doctorId, Guid patientId, DateOnly issuedDate,
        string? diagnosis = null, bool isRefillable = false, int refillsAllowed = 0)
    {
        if (doctorId == Guid.Empty)
            throw new ArgumentException("DoctorId is required.", nameof(doctorId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId is required.", nameof(patientId));
        if (isRefillable && refillsAllowed <= 0)
            throw new ArgumentException("A refillable prescription must allow at least one refill.", nameof(refillsAllowed));

        DoctorId = doctorId;
        PatientId = patientId;
        Diagnosis = diagnosis?.Trim();
        IssuedDate = issuedDate;
        Status = PrescriptionStatus.Pending;
        IsRefillable = isRefillable;
        RefillsAllowed = refillsAllowed;
        RefillsUsed = 0;

        RaiseDomainEvent(new PrescriptionCreatedEvent(Id, DateTime.UtcNow));
    }

    public void AddItem(Guid medicineVariantId, int prescribedQuantity, string? dosageInstructions = null)
    {
        if (Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Expired)
            throw new InvalidPrescriptionStatusException($"Cannot add items to a prescription in '{Status}' status.");

        _items.Add(new PrescriptionItem(Id, medicineVariantId, prescribedQuantity, dosageInstructions));
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

    public void ApplyDispensedQuantities(IReadOnlyDictionary<Guid, int> quantitiesByPrescriptionItemId)
    {
        foreach (var (itemId, quantity) in quantitiesByPrescriptionItemId)
        {
            var item = _items.SingleOrDefault(i => i.Id == itemId)
                ?? throw new InvalidPrescriptionStatusException(
                    $"Prescription item '{itemId}' does not belong to prescription '{Id}'.");

            item.RecordDispensed(quantity);
        }

        Status = _items.All(i => i.IsFullyDispensed)
            ? PrescriptionStatus.FullyDispensed
            : PrescriptionStatus.PartiallyDispensed;

        RaiseDomainEvent(new PrescriptionDispensedEvent(
            Id,
            DateTime.UtcNow,
            quantitiesByPrescriptionItemId.Sum(kv => kv.Value)));
    }

    public void EnsureEligibleForRefill()
    {
        if (!IsRefillable)
            throw new RefillNotEligibleException($"Prescription '{Id}' is not marked as refillable.");
        if (Status != PrescriptionStatus.FullyDispensed)
            throw new RefillNotEligibleException($"Prescription '{Id}' must be fully dispensed before it can be refilled.");
        if (RefillsUsed >= RefillsAllowed)
            throw new RefillNotEligibleException(
                $"Prescription '{Id}' has no refills remaining ({RefillsUsed}/{RefillsAllowed} used).");
    }

    public void RegisterRefill()
    {
        EnsureEligibleForRefill();
        RefillsUsed++;

        foreach (var item in _items)
            item.ResetForRefill();

        Status = PrescriptionStatus.Pending;
        RaiseDomainEvent(new PrescriptionRefilledEvent(Id, DateTime.UtcNow));
    }
}