using Domain.Common;
using Domain.Entities.Prescriptions;
using LicenseNumberVo = Domain.ValueObjects.LicenseNumber;

namespace Domain.Entities.Staff;

public class Doctor : BaseEntity
{
    public Guid UserId { get; private set; }
    public LicenseNumberVo LicenseNumber { get; private set; } = null!;
    public string? Specialization { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Prescription> _prescriptions = new();
    public IReadOnlyCollection<Prescription> Prescriptions => _prescriptions.AsReadOnly();

    private Doctor() { }

    public Doctor(Guid userId, string licenseNumber, string? specialization = null, string? phoneNumber = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
        LicenseNumber = LicenseNumberVo.Of(licenseNumber);
        Specialization = specialization?.Trim();
        PhoneNumber = phoneNumber?.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public bool Owns(Prescription prescription)
    {
        ArgumentNullException.ThrowIfNull(prescription);
        return prescription.DoctorId == Id;
    }
}