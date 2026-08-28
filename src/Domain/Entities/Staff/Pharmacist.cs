using Domain.Common;
using LicenseNumberVo = Domain.ValueObjects.LicenseNumber;

namespace Domain.Entities.Staff;

public class Pharmacist : BaseEntity
{
    public Guid UserId { get; private set; }
    public LicenseNumberVo LicenseNumber { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private Pharmacist() { }

    public Pharmacist(Guid userId, string licenseNumber)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
        LicenseNumber = LicenseNumberVo.Of(licenseNumber);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}