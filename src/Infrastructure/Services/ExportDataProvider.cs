using Application.Common.Interfaces;
using Domain.Enums;
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
        var medicines = await _db.Medicines.AsNoTracking()
            .Include(m => m.GenericName)
            .Include(m => m.Variants).ThenInclude(v => v.Batches)
            .ToListAsync(ct);

        var rows = new List<MedicineExportRow>();
        foreach (var m in medicines)
        {
            if (m.Variants.Count == 0)
            {
                rows.Add(new MedicineExportRow(m.Name, m.GenericName.Name, m.CategoryEnum.ToDisplayValue(), "-", "-", 0, m.IsActive));
            }
            else
            {
                foreach (var v in m.Variants)
                {
                    var stock = v.Batches.Where(b => !b.IsDeleted).Sum(b => b.QuantityAvailable.Value);
                    rows.Add(new MedicineExportRow(m.Name, m.GenericName.Name, m.CategoryEnum.ToDisplayValue(), v.Form.ToString(), $"{v.Strength} {v.Unit}", stock, m.IsActive));
                }
            }
        }
        return rows;
    }

    public async Task<IReadOnlyList<InventoryExportRow>> GetInventoryAsync(CancellationToken ct = default)
    {
        var batches = await _db.MedicineBatches.AsNoTracking()
            .Include(b => b.MedicineVariant).ThenInclude(v => v!.Medicine)
            .ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return batches.Select(b => new InventoryExportRow(
            b.MedicineVariant?.Medicine?.Name ?? "-",
            b.MedicineVariant?.Unit.ToString() ?? "-",
            b.BatchNumber,
            b.QuantityAvailable.Value,
            b.QuantityReceived.Value - b.QuantityAvailable.Value,
            b.ExpiryDate,
            b.ExpiryDate < today ? "Expired" : b.ExpiryDate <= today.AddDays(30) ? "NearExpiry" : "Valid"
        )).ToList();
    }

    public async Task<IReadOnlyList<PrescriptionExportRow>> GetPrescriptionsAsync(CancellationToken ct = default, string? id = null)
    {
        IQueryable<Domain.Entities.Prescriptions.Prescription> query = _db.Prescriptions.AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items).ThenInclude(i => i.MedicineVariant).ThenInclude(v => v!.Medicine);
        if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out var guidId))
        {
            query = query.Where(p => p.Id == guidId);
        }
        var list = await query.ToListAsync(ct);
        // Get doctor names from identity users for display
        var userIds = list.Select(p => p.Doctor?.UserId ?? Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
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
            var doctorName = p.Doctor != null ? (usersDict.TryGetValue(p.Doctor.UserId, out var name) && !string.IsNullOrWhiteSpace(name) ? name : (p.Doctor.LicenseNumber?.Value ?? p.Doctor.UserId.ToString()[..8])) : "-";
            var itemsDesc = string.Join("; ", p.Items.Select(i => {
                var medName = i.MedicineVariant?.Medicine?.Name ?? i.MedicineVariant?.Medicine?.GenericName?.Name ?? "-";
                return $"{medName} x{i.PrescribedQuantity.Value}" + (string.IsNullOrEmpty(i.DosageInstructions) ? "" : $" ({i.DosageInstructions})");
            }));
            return new PrescriptionExportRow(
                p.Id.ToString()[..8],
                p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}" : p.PatientId.ToString()[..8],
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
        var records = await _db.DispensingRecords.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.MedicineBatch).ThenInclude(b => b!.MedicineVariant).ThenInclude(v => v!.Medicine)
            .ToListAsync(ct);
        var rows = new List<DispensingExportRow>();
        foreach (var r in records)
        {
            foreach (var i in r.Items)
            {
                rows.Add(new DispensingExportRow(
                    r.PrescriptionId.ToString()[..8],
                    i.MedicineBatch?.MedicineVariant?.Medicine?.Name ?? i.MedicineBatchId.ToString()[..8],
                    i.Quantity.Value,
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
