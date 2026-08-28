namespace Application.Common.Interfaces;

/// <summary>
/// Resolves the Doctor/Pharmacist profile linked to an Identity user, which is
/// how the system knows 'who is acting' in the pharmacy domain records.
/// </summary>
public interface IStaffService
{
    Task<Guid?> GetDoctorIdForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> GetPharmacistIdForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(Guid Id, string FullName)?> GetDoctorAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(Guid Id, string FullName)?> GetPharmacistAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Looks up a staff member's name (from the linked user account) by its domain id.</summary>
    Task<string?> GetDoctorNameAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<string?> GetPharmacistNameAsync(Guid pharmacistId, CancellationToken cancellationToken = default);

    Task CreateDoctorProfileAsync(Guid userId, string licenseNumber, string? specialization, string? phoneNumber, CancellationToken cancellationToken = default);
    Task CreatePharmacistProfileAsync(Guid userId, string licenseNumber, CancellationToken cancellationToken = default);
}