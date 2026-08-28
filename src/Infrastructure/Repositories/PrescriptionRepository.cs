using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using Application.Features.Dispensing.Queries;
using Application.Features.Prescriptions.Dtos;
using Application.Features.Prescriptions.Queries;
using Domain.Entities.Dispensing;
using Domain.Entities.Prescriptions;
using Domain.Entities.Staff;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PrescriptionRepository : BaseRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<Prescription?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<Prescription>()
            .Include(p => p.Patient)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Prescription?> GetByIdWithItemsAndDoctorAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<Prescription>()
            .Include(p => p.Patient)
            .Include(p => p.Items)
            .Include(p => p.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<PrescriptionListItemDto>> ListAsync(
        ListPrescriptionsQuery query,
        Guid? restrictedToDoctorId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Prescription> data = Db.Set<Prescription>().AsNoTracking();

        if (restrictedToDoctorId.HasValue)
            data = data.Where(p => p.DoctorId == restrictedToDoctorId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            // NOTE: Uses SQL LIKE via Contains ("%search%"). For large patient/prescription tables consider full-text search or trigram indexes.
            data = data.Where(p => p.Patient != null &&
                (p.Patient.FirstName.Contains(search) || p.Patient.LastName.Contains(search)));
        }

        if (query.Status.HasValue)
            data = data.Where(p => p.Status == query.Status.Value);

        if (query.FromDate.HasValue)
            data = data.Where(p => p.IssuedDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            data = data.Where(p => p.IssuedDate <= query.ToDate.Value);

        var totalCount = await data.CountAsync(cancellationToken);

        data = query.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => SortDir(data, p => p.CreatedAt, query.SortDir),
            "patientname" => SortDir(data, p => p.Patient != null ? (p.Patient.LastName + " " + p.Patient.FirstName) : string.Empty, query.SortDir),
            "status" => SortDir(data, p => p.Status, query.SortDir),
            _ => SortDir(data, p => p.IssuedDate, query.SortDir)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Project prescriptions to a lightweight shape; compute item counts in SQL and avoid Include.
        var projected = data
            .Select(p => new
            {
                p.Id,
                p.DoctorId,
                PatientName = p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
                PatientDateOfBirth = p.Patient != null ? p.Patient.DateOfBirth : default(DateOnly),
                PatientAge = p.Patient != null ? p.Patient.Age : 0,
                PatientPhone = p.Patient != null ? p.Patient.PhoneNumber : null,
                p.IssuedDate,
                Status = p.Status.ToString(),
                p.IsRefillable,
                ItemCount = p.Items.Count()
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var pageItems = await projected.ToListAsync(cancellationToken);

        var doctorIds = pageItems.Select(p => p.DoctorId).Distinct().Where(id => id != Guid.Empty).ToList();
        var doctorNamesById = doctorIds.Count == 0 ? new Dictionary<Guid, string>() : await ResolveDoctorNamesAsync(doctorIds, cancellationToken);

        var items = pageItems
            .Select(p => new PrescriptionListItemDto(
                p.Id,
                p.DoctorId,
                doctorNamesById.TryGetValue(p.DoctorId, out var doctorName) ? doctorName : string.Empty,
                p.PatientName,
                p.PatientDateOfBirth,
                p.PatientAge,
                p.PatientPhone,
                p.IssuedDate,
                p.Status,
                p.IsRefillable,
                p.ItemCount))
            .ToList();

        return new PagedResult<PrescriptionListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<DispensingRecordDto>> ListDispensingRecordsAsync(
        DispensingRecordListQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<DispensingRecord> data = Db.Set<DispensingRecord>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            // NOTE: Uses SQL LIKE via Contains ("%search%"). If dispensing records grow large consider full-text search or trigram indexes for patient/notes search.
            data = data.Where(r =>
                (r.Prescription != null && r.Prescription.Patient != null &&
                 (r.Prescription.Patient.FirstName.Contains(search) || r.Prescription.Patient.LastName.Contains(search))) ||
                (r.Notes != null && r.Notes.Contains(search)));
        }

        if (query.FromDate.HasValue)
            data = data.Where(r => r.DispensedAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            data = data.Where(r => r.DispensedAt <= query.ToDate.Value);

        var totalCount = await data.CountAsync(cancellationToken);

        data = SortDir(data, r => r.DispensedAt, query.SortDir);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Project dispensing records and their items; avoid loading full entity graphs with Include.
        var projected = data
            .Select(r => new
            {
                r.Id,
                r.PrescriptionId,
                PatientName = r.Prescription != null && r.Prescription.Patient != null ? r.Prescription.Patient.FullName : string.Empty,
                r.PharmacistId,
                r.DispensedAt,
                r.Notes,
                Items = r.Items.Select(i => new
                {
                    i.MedicineBatchId,
                    MedicineName = i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null && i.MedicineBatch.MedicineVariant.Medicine != null
                        ? i.MedicineBatch.MedicineVariant.Medicine.Name : "Unknown",
                    VariantName = i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null
                        ? $"{i.MedicineBatch.MedicineVariant.Form} {i.MedicineBatch.MedicineVariant.Strength} {i.MedicineBatch.MedicineVariant.Unit}"
                        : string.Empty,
                    BatchNumber = i.MedicineBatch != null ? i.MedicineBatch.BatchNumber : string.Empty,
                    Quantity = i.Quantity.Value
                }).ToList()
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await projected.ToListAsync(cancellationToken);

        var pharmacistIds = rows.Select(r => r.PharmacistId).Distinct().ToList();
        var pharmacistNamesById = pharmacistIds.Count == 0 ? new Dictionary<Guid, string>() : await ResolvePharmacistNamesAsync(pharmacistIds, cancellationToken);

        // Fetch items for the page in a separate query to avoid correlated subqueries per row
        var recordIds = rows.Select(r => r.Id).ToList();
        var itemsByRecord = recordIds.Count == 0
            ? new Dictionary<Guid, List<DispensingRecordItemDto>>()
            : (await Db.Set<DispensingRecordItem>()
                .AsNoTracking()
                .Where(i => recordIds.Contains(i.DispensingRecordId))
                .Select(i => new
                {
                    i.DispensingRecordId,
                    i.MedicineBatchId,
                    MedicineName = i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null && i.MedicineBatch.MedicineVariant.Medicine != null
                        ? i.MedicineBatch.MedicineVariant.Medicine.Name : "Unknown",
                    VariantName = i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null
                        ? $"{i.MedicineBatch.MedicineVariant.Form} {i.MedicineBatch.MedicineVariant.Strength} {i.MedicineBatch.MedicineVariant.Unit}"
                        : string.Empty,
                    BatchNumber = i.MedicineBatch != null ? i.MedicineBatch.BatchNumber : string.Empty,
                    Quantity = i.Quantity.Value
                })
                .ToListAsync(cancellationToken))
                .GroupBy(x => x.DispensingRecordId)
                .ToDictionary(g => g.Key, g => g.Select(i => new DispensingRecordItemDto(i.MedicineBatchId, i.MedicineName, i.VariantName, i.BatchNumber, i.Quantity)).ToList());

        var items = rows.Select(r => new DispensingRecordDto(
            r.Id,
            r.PrescriptionId,
            r.PatientName,
            r.PharmacistId,
            pharmacistNamesById.TryGetValue(r.PharmacistId, out var pharmacistName) ? pharmacistName : string.Empty,
            r.DispensedAt,
            r.Notes,
            itemsByRecord.TryGetValue(r.Id, out var recItems) ? recItems : new List<DispensingRecordItemDto>()))
            .ToList();

        return new PagedResult<DispensingRecordDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public void AddDispensingRecord(DispensingRecord record)
        => Db.Set<DispensingRecord>().Add(record);

    private async Task<Dictionary<Guid, string>> ResolveDoctorNamesAsync(
        List<Guid> doctorIds,
        CancellationToken cancellationToken)
        => await (from d in Db.Set<Doctor>()
                  join u in Db.Users on d.UserId equals u.Id
                  where doctorIds.Contains(d.Id)
                  select new { d.Id, Name = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

    private async Task<Dictionary<Guid, string>> ResolvePharmacistNamesAsync(
        List<Guid> pharmacistIds,
        CancellationToken cancellationToken)
        => await (from ph in Db.Set<Pharmacist>()
                  join u in Db.Users on ph.UserId equals u.Id
                  where pharmacistIds.Contains(ph.Id)
                  select new { ph.Id, Name = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}