using Application.Common.Interfaces;
using Domain.Entities.Staff;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class StaffService : IStaffService
{
    private readonly ApplicationDbContext _db;

    public StaffService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid?> GetDoctorIdForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _db.Set<Doctor>()
            .Where(d => d.UserId == userId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(Guid Id, string FullName)?> GetPharmacistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pharmacist = await (from ph in _db.Set<Pharmacist>()
                                join u in _db.Users on ph.UserId equals u.Id
                                where ph.UserId == userId
                                select new { ph.Id, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .FirstOrDefaultAsync(cancellationToken);

        return pharmacist is null ? null : (pharmacist.Id, pharmacist.FullName);
    }

    public Task<string?> GetDoctorNameAsync(Guid doctorId, CancellationToken cancellationToken = default)
        => (from d in _db.Set<Doctor>()
            join u in _db.Users on d.UserId equals u.Id
            where d.Id == doctorId
            select (u.FirstName + " " + u.LastName).Trim()).FirstOrDefaultAsync(cancellationToken);

    /// <summary>All requested doctor names in one round trip (WHERE IN).</summary>
    public async Task<IReadOnlyDictionary<Guid, string>> GetDoctorNamesAsync(
        IEnumerable<Guid> doctorIds,
        CancellationToken cancellationToken = default)
    {
        var ids = doctorIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await (from d in _db.Set<Doctor>()
                      join u in _db.Users on d.UserId equals u.Id
                      where ids.Contains(d.Id)
                      select new { d.Id, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
    }

    /// <summary>All requested pharmacist names in one round trip (WHERE IN).</summary>
    public async Task<IReadOnlyDictionary<Guid, string>> GetPharmacistNamesAsync(
        IEnumerable<Guid> pharmacistIds,
        CancellationToken cancellationToken = default)
    {
        var ids = pharmacistIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await (from ph in _db.Set<Pharmacist>()
                      join u in _db.Users on ph.UserId equals u.Id
                      where ids.Contains(ph.Id)
                      select new { ph.Id, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
    }

    public Task CreateDoctorProfileAsync(Guid userId, string licenseNumber, string? specialization, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        _db.Set<Doctor>().Add(new Doctor(userId, licenseNumber, specialization, phoneNumber));
        return Task.CompletedTask;
    }

    public Task CreatePharmacistProfileAsync(Guid userId, string licenseNumber, CancellationToken cancellationToken = default)
    {
        _db.Set<Pharmacist>().Add(new Pharmacist(userId, licenseNumber));
        return Task.CompletedTask;
    }
}