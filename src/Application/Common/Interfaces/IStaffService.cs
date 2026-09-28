namespace Application.Common.Interfaces;

public interface IStaffService
{
    Task<Guid?> GetDoctorIdForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(Guid Id, string FullName)?> GetPharmacistAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<string?> GetDoctorNameAsync(Guid doctorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetDoctorNamesAsync(
        IEnumerable<Guid> doctorIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetPharmacistNamesAsync(
        IEnumerable<Guid> pharmacistIds,
        CancellationToken cancellationToken = default);

    Task CreateDoctorProfileAsync(Guid userId, string licenseNumber, string? specialization, string? phoneNumber, CancellationToken cancellationToken = default);
    Task CreatePharmacistProfileAsync(Guid userId, string licenseNumber, CancellationToken cancellationToken = default);
}