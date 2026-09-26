using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Application.Features.Medicines.Dtos;

namespace Infrastructure.Services;

public class ExportDataProvider : IExportDataProvider
{
    private readonly ApplicationDbContext _db;
    public ExportDataProvider(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MedicineExportRow>> GetMedicinesAsync(CancellationToken ct = default)
    {
        // Lean projection: medicine name + scientific GenericName + variant
        // type/stock as scalar columns and server-side SUMs (no Include, no
        // entity graph, no audit fields). Display strings are built in memory.
        var projected = await _db.Medicines.AsNoTracking()
            .Select(m => new
            {
                m.Name,
                GenericName = m.GenericName.Name,
                m.CategoryEnum,
                m.IsActive,
                Variants = m.Variants
                    .Select(v => new
                    {
                        v.Form,
                        v.Strength,
                        v.Unit,
                        Stock = v.Batches.Sum(b => (int?)b.QuantityAvailable.Value) ?? 0
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var rows = new List<MedicineExportRow>();
        foreach (var m in projected)
        {
            if (m.Variants.Count == 0)
            {
                rows.Add(new MedicineExportRow(m.Name, m.GenericName, m.CategoryEnum.ToString(), "-", "-", 0, m.IsActive));
            }
            else
            {
                foreach (var v in m.Variants)
                {
                    rows.Add(new MedicineExportRow(m.Name, m.GenericName, m.CategoryEnum.ToString(), v.Form.ToString(), $"{v.Strength} {v.Unit}", v.Stock, m.IsActive));
                }
            }
        }
        return rows;
    }

    public async Task<IReadOnlyList<InventoryExportRow>> GetInventoryAsync(CancellationToken ct = default)
    {
        // Lean projection: medicine name + variant unit + batch columns only.
        // Status is derived in memory (date math, not SQL).
        var projected = await _db.MedicineBatches.AsNoTracking()
            .Select(b => new
            {
                MedicineName = b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.Name : "-",
                Unit = (Domain.Enums.MedicineUnit?)b.MedicineVariant!.Unit,
                b.BatchNumber,
                Available = b.QuantityAvailable.Value,
                Received = b.QuantityReceived.Value,
                b.ExpiryDate
            })
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return projected.Select(b => new InventoryExportRow(
            b.MedicineName,
            b.Unit != null ? b.Unit.ToString()! : "-",
            b.BatchNumber,
            b.Available,
            b.Received - b.Available,
            b.ExpiryDate,
            b.ExpiryDate < today ? "Expired" : b.ExpiryDate <= today.AddDays(30) ? "NearExpiry" : "Valid"
        )).ToList();
    }

    public async Task<IReadOnlyList<PrescriptionExportRow>> GetPrescriptionsAsync(CancellationToken ct = default, string? id = null)
    {
        // Lean projection: patient name/info + doctor key + item medicine names
        // as scalar columns (no Patient/Doctor/Variant/Medicine entity loads).
        // The GenericName join is part of the projection, so the
        // Medicine -> GenericName fallback works (it never did with Include).
        var query = _db.Prescriptions.AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.PatientId,
                PatientFirstName = p.Patient != null ? p.Patient.FirstName : null,
                PatientLastName = p.Patient != null ? p.Patient.LastName : null,
                DoctorUserId = p.Doctor != null ? (Guid?)p.Doctor.UserId : null,
                DoctorLicense = p.Doctor != null ? p.Doctor.LicenseNumber!.Value : null,
                p.Status,
                p.IssuedDate,
                Items = p.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new
                    {
                        MedicineName = i.MedicineVariant!.Medicine != null ? i.MedicineVariant.Medicine.Name : null,
                        GenericName = i.MedicineVariant!.Medicine != null && i.MedicineVariant.Medicine.GenericName != null
                            ? i.MedicineVariant.Medicine.GenericName.Name : null,
                        PrescribedQty = i.PrescribedQuantity.Value,
                        i.DosageInstructions
                    })
                    .ToList()
            });

        if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out var guidId))
        {
            query = query.Where(p => p.Id == guidId);
        }
        var list = await query.ToListAsync(ct);

        // Get doctor names from identity users for display
        var userIds = list.Select(p => p.DoctorUserId ?? Guid.Empty).Where(uid => uid != Guid.Empty).Distinct().ToList();
        var usersDict = new Dictionary<Guid, string>();
        if (userIds.Any())
        {
            var users = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, FullName = (u.FirstName ?? "") + " " + (u.LastName ?? "") })
                .ToListAsync(ct);
            usersDict = users.ToDictionary(u => u.Id, u => (u.FullName ?? "").Trim());
        }
        return list.Select(p => {
            var doctorName = p.DoctorUserId != null ? (usersDict.TryGetValue(p.DoctorUserId.Value, out var name) && !string.IsNullOrWhiteSpace(name) ? name : (p.DoctorLicense ?? p.DoctorUserId.Value.ToString()[..8])) : "-";
            var itemsDesc = string.Join("; ", p.Items.Select(i => {
                var medName = i.MedicineName ?? i.GenericName ?? "-";
                return $"{medName} x{i.PrescribedQty}" + (string.IsNullOrEmpty(i.DosageInstructions) ? "" : $" ({i.DosageInstructions})");
            }));
            return new PrescriptionExportRow(
                p.Id.ToString()[..8],
                p.PatientFirstName != null ? $"{p.PatientFirstName} {p.PatientLastName}" : p.PatientId.ToString()[..8],
                doctorName,
                p.Status.ToString(),
                p.IssuedDate.ToDateTime(TimeOnly.MinValue),
                p.Items.Count,
                itemsDesc
            );
        }).ToList();
    }

    public async Task<IReadOnlyList<DispensingExportRow>> GetDispensingAsync(CancellationToken ct = default)
    {
        // Lean projection: record keys + per-line medicine name + quantity
        // (no record/item/batch/variant/medicine entity graphs).
        var records = await _db.DispensingRecords.AsNoTracking()
            .Select(r => new
            {
                r.Id,
                r.PrescriptionId,
                r.DispensedAt,
                r.PharmacistId,
                Items = r.Items.Select(i => new
                {
                    i.MedicineBatchId,
                    MedicineName = i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null && i.MedicineBatch.MedicineVariant.Medicine != null
                        ? i.MedicineBatch.MedicineVariant.Medicine.Name : null,
                    Quantity = i.Quantity.Value
                }).ToList()
            })
            .ToListAsync(ct);

        var rows = new List<DispensingExportRow>();
        foreach (var r in records)
        {
            foreach (var i in r.Items)
            {
                rows.Add(new DispensingExportRow(
                    r.PrescriptionId.ToString()[..8],
                    i.MedicineName ?? i.MedicineBatchId.ToString()[..8],
                    i.Quantity,
                    r.DispensedAt,
                    r.PharmacistId.ToString()[..8]
                ));
            }
        }
        if (rows.Count == 0 && records.Count > 0)
        {
            // fallback if items empty
            rows.AddRange(records.Select(r => new DispensingExportRow(r.PrescriptionId.ToString()[..8], "-", 0, r.DispensedAt, r.PharmacistId.ToString()[..8])));
        }
        return rows;
    }
}
