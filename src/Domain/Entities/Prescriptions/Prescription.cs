using System.Security.Cryptography;
using Domain.Common;
using Domain.Entities.Patients;
using Domain.Entities.Staff;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;

namespace Domain.Entities.Prescriptions;

public class Prescription : BaseEntity
{
    public const int ShortCodeLength = 8;

    internal static readonly char[] ShortCodeAlphabet =
        "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

    public Guid DoctorId { get; private set; }
    public Doctor? Doctor { get; private set; }

    public Guid PatientId { get; private set; }
    public Patient? Patient { get; private set; }
    public string ShortCode { get; private set; } = GenerateShortCode();
    public string? Diagnosis { get; private set; }
    public DateOnly IssuedDate { get; private set; }
    public PrescriptionStatus Status { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    private readonly List<PrescriptionItem> _items = new();
    public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();

    private Prescription() { }

    public static string GenerateShortCode(int length = ShortCodeLength)
    {
        if (length is < 6 or > 12)
            throw new ArgumentOutOfRangeException(nameof(length), "Short code length must be between 6 and 12.");

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = ShortCodeAlphabet[bytes[i] % ShortCodeAlphabet.Length];
        return new string(chars);
    }

    public void RegenerateShortCode()
        => ShortCode = GenerateShortCode();

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
            throw new InvalidPrescriptionStatusException(PrescriptionStatusReason.AddItemsProhibited, Status, Id);

        _items.Add(new PrescriptionItem(Id, medicineVariantId, prescribedQuantity, dosageInstructions, isRefillable, refillsAllowed, refillIntervalDays));
    }

    public void Cancel()
    {
        if (Status == PrescriptionStatus.Cancelled)
            throw new InvalidPrescriptionStatusException(PrescriptionStatusReason.AlreadyCancelled, Status, Id);
        if (Status == PrescriptionStatus.FullyDispensed)
            throw new InvalidPrescriptionStatusException(PrescriptionStatusReason.CancelAfterDispensed, Status, Id);

        Status = PrescriptionStatus.Cancelled;
        RaiseDomainEvent(new PrescriptionCancelledEvent(Id, DateTime.UtcNow));
    }

    public void EnsureCanBeDispensed(DateOnly asOf)
    {
        if (Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Expired or PrescriptionStatus.FullyDispensed)
            throw new InvalidPrescriptionStatusException(PrescriptionStatusReason.NotDispensable, Status, Id);

        if (Items.Count == 0)
            throw new InvalidPrescriptionStatusException(PrescriptionStatusReason.Empty, Status, Id);
    }

    public void ApplyDispensedQuantities(
        IReadOnlyDictionary<Guid, int> quantitiesByPrescriptionItemId,
        DateOnly dispensedOn)
    {
        foreach (var (itemId, quantity) in quantitiesByPrescriptionItemId)
        {
            var item = _items.SingleOrDefault(i => i.Id == itemId)
                ?? throw new InvalidPrescriptionStatusException(
                    PrescriptionStatusReason.ItemNotInPrescription, Status, Id, itemId);

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

    public void RegisterItemsRefill(IReadOnlyCollection<Guid> prescriptionItemIds)
    {
        if (Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Expired)
            throw new InvalidPrescriptionStatusException(PrescriptionStatusReason.RefillProhibited, Status, Id);

        if (prescriptionItemIds.Count == 0)
            throw new ArgumentException("At least one prescription item is required.", nameof(prescriptionItemIds));

        var distinctIds = prescriptionItemIds.Distinct().ToList();
        var targets = new List<PrescriptionItem>(distinctIds.Count);
        foreach (var itemId in distinctIds)
        {
            var item = _items.SingleOrDefault(i => i.Id == itemId)
                ?? throw new InvalidPrescriptionStatusException(
                    PrescriptionStatusReason.ItemNotInPrescription, Status, Id, itemId);

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