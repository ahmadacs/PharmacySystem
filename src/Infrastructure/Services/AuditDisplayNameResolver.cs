using Application.Common.Interfaces;
using Domain.Entities.Audit;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DispensingRecord = Domain.Entities.Dispensing.DispensingRecord;
using DispensingRecordItem = Domain.Entities.Dispensing.DispensingRecordItem;
using Doctor = Domain.Entities.Staff.Doctor;
using FileAttachment = Domain.Entities.Files.FileAttachment;
using GenericName = Domain.Entities.Medicines.GenericName;
using InventoryAdjustment = Domain.Entities.Inventory.InventoryAdjustment;
using Medicine = Domain.Entities.Medicines.Medicine;
using MedicineBatch = Domain.Entities.Medicines.MedicineBatch;
using MedicineVariant = Domain.Entities.Medicines.MedicineVariant;
using Patient = Domain.Entities.Patients.Patient;
using Pharmacist = Domain.Entities.Staff.Pharmacist;
using Prescription = Domain.Entities.Prescriptions.Prescription;
using PrescriptionItem = Domain.Entities.Prescriptions.PrescriptionItem;

namespace Infrastructure.Services;

/// <summary>
/// Batch-resolves human-readable labels for audit targets, one query per entity
/// type. Soft-deleted rows are included (IgnoreQueryFilters) so history stays
/// readable even after the entity was deleted.
/// </summary>
public sealed class AuditDisplayNameResolver : IAuditDisplayNameResolver
{
    private readonly ApplicationDbContext _db;

    public AuditDisplayNameResolver(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AuditDisplayResult> ResolvePageAsync(
        IReadOnlyList<AuditEntry> entries,
        IReadOnlyCollection<AuditValueRef> valueRefs,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0 && valueRefs.Count == 0 && userIds.Count == 0)
            return new AuditDisplayResult(
                new Dictionary<Guid, string>(),
                new Dictionary<AuditValueRef, string>(),
                new Dictionary<Guid, string>());

        // 1. Merge entry targets and FK values per entity type: each table is
        // queried at most once even when it appears as both (e.g. Patient is
        // both an entry target and a PatientId change value).
        var idsByEntity = new Dictionary<string, HashSet<Guid>>(StringComparer.Ordinal);
        void Add(string entity, Guid id)
        {
            if (!idsByEntity.TryGetValue(entity, out var set))
                idsByEntity[entity] = set = [];
            set.Add(id);
        }

        foreach (var entry in entries)
            Add(entry.EntityName, entry.EntityId);
        foreach (var r in valueRefs)
        {
            if (ValueEntityNames.TryGetValue(r.Property, out var entity) && entity != UserEntityName)
                Add(entity, r.ValueId);
        }

        // 2. One query per entity type (sequential awaits: shared DbContext).
        // Staff rows are fetched first; their linked users join the single
        // users query below instead of triggering one per staff type.
        var labelsByEntity = new Dictionary<string, Dictionary<Guid, string>>(StringComparer.Ordinal);
        var staffRows = new Dictionary<string, List<StaffRef>>(StringComparer.Ordinal);
        var staffUserIds = new HashSet<Guid>();
        foreach (var (entity, ids) in idsByEntity)
        {
            if (entity is nameof(Doctor) or nameof(Pharmacist))
            {
                var rows = await StaffRefsAsync(entity, ids.ToList(), cancellationToken);
                staffRows[entity] = rows;
                foreach (var u in rows.Select(r => r.UserId))
                    staffUserIds.Add(u);
            }
            else
            {
                labelsByEntity[entity] = await LabelsForAsync(entity, ids.ToList(), cancellationToken);
            }
        }

        // 3. Exactly one users query: authors + staff-linked + FK user refs.
        var allUserIds = userIds
            .Concat(staffUserIds)
            .Concat(valueRefs
                .Where(r => ValueEntityNames.TryGetValue(r.Property, out var e) && e == UserEntityName)
                .Select(r => r.ValueId))
            .Distinct()
            .ToList();
        var userNames = await UserLabelsAsync(allUserIds, cancellationToken);

        // 4. Staff labels now that names are known (fallback keeps them readable).
        foreach (var (entity, rows) in staffRows)
        {
            var role = entity == nameof(Doctor) ? "Doctor" : "Pharmacist";
            labelsByEntity[entity] = rows.ToDictionary(
                r => r.Id,
                r => userNames.TryGetValue(r.UserId, out var name) && !string.IsNullOrEmpty(name)
                    ? name
                    : $"{role} {Short(r.Id)}");
        }

        // 5. Distribute labels back to entries and change values.
        var entryDisplays = new Dictionary<Guid, string>();
        foreach (var entry in entries)
        {
            if (labelsByEntity.TryGetValue(entry.EntityName, out var labels)
                && labels.TryGetValue(entry.EntityId, out var label))
                entryDisplays[entry.Id] = label;
        }

        var valueDisplays = new Dictionary<AuditValueRef, string>();
        foreach (var r in valueRefs)
        {
            if (!ValueEntityNames.TryGetValue(r.Property, out var entity))
                continue;
            if (entity == UserEntityName)
            {
                if (userNames.TryGetValue(r.ValueId, out var name))
                    valueDisplays[r] = name;
            }
            else if (labelsByEntity.TryGetValue(entity, out var labels)
                && labels.TryGetValue(r.ValueId, out var label))
            {
                valueDisplays[r] = label;
            }
        }

        return new AuditDisplayResult(entryDisplays, valueDisplays, userNames);
    }

    /// <summary>
    /// Maps a change property holding a FK to the referenced entity type.
    /// Plain strings (not nameof) because the same property name exists on
    /// several entities and always points at the same target (e.g. every
    /// <c>PrescriptionId</c> is a <c>Prescription</c>).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> ValueEntityNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DoctorId"] = nameof(Doctor),
            ["PatientId"] = nameof(Patient),
            ["PrescriptionId"] = nameof(Prescription),
            ["PharmacistId"] = nameof(Pharmacist),
            ["MedicineVariantId"] = nameof(MedicineVariant),
            ["MedicineId"] = nameof(Medicine),
            ["MedicineBatchId"] = nameof(MedicineBatch),
            ["PrescriptionItemId"] = nameof(PrescriptionItem),
            ["DispensingRecordId"] = nameof(DispensingRecord),
            ["GenericNameId"] = nameof(GenericName),
            ["UserId"] = UserEntityName,
            ["AdjustedBy"] = UserEntityName,
            ["ChangedBy"] = UserEntityName,
        };

    private const string UserEntityName = "ApplicationUser";

    private async Task<Dictionary<Guid, string>> UserLabelsAsync(
        List<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var rows = await _db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(ct);
        return rows
            .Select(r => (r.Id, Name: $"{r.FirstName} {r.LastName}".Trim()))
            .Where(x => x.Name.Length > 0)
            .ToDictionary(x => x.Id, x => x.Name);
    }

    private Task<Dictionary<Guid, string>> LabelsForAsync(
        string entityName, List<Guid> ids, CancellationToken ct) => entityName switch
    {
        nameof(Medicine) => LabelsAsync(
            _db.Medicines.IgnoreQueryFilters().Where(m => ids.Contains(m.Id)),
            m => m.Id, m => m.Name, ct),

        nameof(GenericName) => LabelsAsync(
            _db.GenericNames.IgnoreQueryFilters().Where(m => ids.Contains(m.Id)),
            m => m.Id, m => m.Name, ct),

        nameof(FileAttachment) => LabelsAsync(
            _db.FileAttachments.IgnoreQueryFilters().Where(f => ids.Contains(f.Id)),
            f => f.Id, f => f.FileName, ct),

        nameof(Patient) => LabelsAsync(
            _db.Patients.IgnoreQueryFilters().Where(p => ids.Contains(p.Id)),
            p => p.Id, p => $"{p.FirstName} {p.LastName}", ct),

        nameof(MedicineVariant) => LabelsAsync(
            _db.MedicineVariants.IgnoreQueryFilters().Where(v => ids.Contains(v.Id))
                .Select(v => new
                {
                    v.Id,
                    MedicineName = v.Medicine == null ? null : v.Medicine.Name,
                    v.Form,
                    v.Strength,
                    v.Unit
                }),
            r => r.Id,
            r => Combine(r.MedicineName, $"{r.Form} {r.Strength} {r.Unit}"),
            ct),

        nameof(MedicineBatch) => LabelsAsync(
            _db.MedicineBatches.IgnoreQueryFilters().Where(b => ids.Contains(b.Id))
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    MedicineName = b.MedicineVariant == null || b.MedicineVariant.Medicine == null
                        ? null
                        : b.MedicineVariant.Medicine.Name
                }),
            r => r.Id,
            r => Combine($"Batch {r.BatchNumber}", r.MedicineName, reverse: true),
            ct),

        nameof(Prescription) => LabelsAsync(
            _db.Prescriptions.IgnoreQueryFilters().Where(p => ids.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.IssuedDate,
                    PatientName = p.Patient == null ? null : p.Patient.FirstName + " " + p.Patient.LastName
                }),
            r => r.Id,
            r => r.PatientName == null
                ? $"Prescription {r.IssuedDate:yyyy-MM-dd}"
                : Combine(r.PatientName, $"{r.IssuedDate:yyyy-MM-dd}"),
            ct),

        nameof(PrescriptionItem) => LabelsAsync(
            _db.PrescriptionItems.IgnoreQueryFilters().Where(i => ids.Contains(i.Id))
                .Select(i => new
                {
                    i.Id,
                    MedicineName = i.MedicineVariant == null || i.MedicineVariant.Medicine == null
                        ? null
                        : i.MedicineVariant.Medicine.Name
                }),
            r => r.Id,
            r => r.MedicineName ?? $"Item {Short(r.Id)}",
            ct),

        nameof(DispensingRecord) => LabelsAsync(
            _db.DispensingRecords.IgnoreQueryFilters().Where(d => ids.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    d.DispensedAt,
                    PatientName = d.Prescription == null || d.Prescription.Patient == null
                        ? null
                        : d.Prescription.Patient.FirstName + " " + d.Prescription.Patient.LastName
                }),
            r => r.Id,
            r => r.PatientName == null
                ? $"Dispensing {r.DispensedAt:yyyy-MM-dd}"
                : Combine(r.PatientName, $"{r.DispensedAt:yyyy-MM-dd}"),
            ct),

        nameof(DispensingRecordItem) => LabelsAsync(
            _db.DispensingRecordItems.IgnoreQueryFilters().Where(i => ids.Contains(i.Id))
                .Select(i => new
                {
                    i.Id,
                    BatchNumber = i.MedicineBatch == null ? null : i.MedicineBatch.BatchNumber
                }),
            r => r.Id,
            r => r.BatchNumber == null ? $"Item {Short(r.Id)}" : $"Batch {r.BatchNumber}",
            ct),

        nameof(InventoryAdjustment) => LabelsAsync(
            _db.InventoryAdjustments.IgnoreQueryFilters().Where(a => ids.Contains(a.Id))
                .Select(a => new
                {
                    a.Id,
                    a.Type,
                    BatchNumber = a.MedicineBatch == null ? null : a.MedicineBatch.BatchNumber
                }),
            r => r.Id,
            r => r.BatchNumber == null ? r.Type.ToString() : $"{r.Type} — Batch {r.BatchNumber}",
            ct),

        // Doctor/Pharmacist never reach here: ResolvePageAsync intercepts them
        // to fold their linked users into the single users query.
        _ => Task.FromResult(new Dictionary<Guid, string>()),
    };

    /// <summary>Fetches rows then maps them to non-blank labels client-side.</summary>
    private static async Task<Dictionary<Guid, string>> LabelsAsync<T>(
        IQueryable<T> query, Func<T, Guid> id, Func<T, string?> display, CancellationToken ct)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var row in await query.ToListAsync(ct))
        {
            var label = display(row)?.Trim();
            if (!string.IsNullOrEmpty(label))
                map[id(row)] = label;
        }
        return map;
    }

    private sealed record StaffRef(Guid Id, Guid UserId);

    /// <summary>Fetches staff link rows; names resolve via the single users query.</summary>
    private Task<List<StaffRef>> StaffRefsAsync(string entityName, List<Guid> ids, CancellationToken ct)
        => entityName == nameof(Doctor)
            ? _db.Doctors.IgnoreQueryFilters().Where(d => ids.Contains(d.Id))
                .Select(d => new StaffRef(d.Id, d.UserId)).ToListAsync(ct)
            : _db.Pharmacists.IgnoreQueryFilters().Where(p => ids.Contains(p.Id))
                .Select(p => new StaffRef(p.Id, p.UserId)).ToListAsync(ct);

    /// <summary>Joins two label parts, ignoring blanks: Combine(a, b) => "a — b".</summary>
    private static string? Combine(string? first, string? second, bool reverse = false)
    {
        var left = (reverse ? second : first)?.Trim();
        var right = (reverse ? first : second)?.Trim();
        return (left, right) switch
        {
            (null or "", null or "") => null,
            (null or "", _) => right,
            (_, null or "") => left,
            _ => $"{left} — {right}"
        };
    }

    private static string Short(Guid id) => id.ToString("N")[..8].ToUpperInvariant();
}
