using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Application.Features.Inventory.Queries;
using Application.Features.Medicines.Dtos;
using Application.Features.Medicines.Queries;
using Domain.Entities.Dispensing;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MedicineRepository : BaseRepository<Medicine>, IMedicineRepository
{
    public MedicineRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<GenericName> GetOrCreateGenericNameAsync(string name, string? nameAr = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Generic (scientific) name is required.", nameof(name));

        var trimmed = name.Trim();
        var trimmedAr = nameAr?.Trim();
        var existing = await Db.Set<GenericName>().FirstOrDefaultAsync(g => g.Name == trimmed, cancellationToken);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(trimmedAr) && existing.NameAr != trimmedAr)
            {
                existing.Rename(trimmed, trimmedAr);
            }
            return existing;
        }

        var genericName = new GenericName(trimmed, trimmedAr);
        // Add only to the DbContext. The Unit of Work / caller is responsible for SaveChanges.
        Db.Set<GenericName>().Add(genericName);
        return genericName;
    }

    public async Task<Medicine?> GetByIdWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<Medicine>()
            .AsNoTracking()
            .Include(m => m.Variants)
                .ThenInclude(v => v.Batches)
            .Include(m => m.GenericName)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<MedicineVariant?> GetVariantByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<MedicineVariant?> FindVariantAsync(Guid medicineId, MedicineForm form, MedicineUnit unit, decimal strength, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.MedicineId == medicineId && v.Form == form && v.Unit == unit && v.Strength == strength, cancellationToken);

    public async Task<List<MedicineVariant>> GetVariantsByIdsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>()
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

    public async Task<List<MedicineVariant>> GetForDispensingAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineVariant>()
            .Include(v => v.Batches)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

    public async Task<List<Medicine>> GetMedicinesByVariantIdsForStockCheckAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default)
        => await Db.Set<Medicine>()
            .Include(m => m.Variants)
                .ThenInclude(v => v.Batches)
            .Where(m => m.Variants.Any(v => variantIds.Contains(v.Id)))
            .ToListAsync(cancellationToken);

    public void AddVariant(MedicineVariant variant)
        => Db.Set<MedicineVariant>().Add(variant);

    public void RemoveVariant(MedicineVariant variant)
        => Db.Set<MedicineVariant>().Remove(variant);

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = Enum.GetValues<CategoryEnum>()
            .Select(c => new CategoryDto((int)c, c.ToDisplayValue(), null))
            .ToList();
        return categories;
    }

    public async Task<MedicineBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Set<MedicineBatch>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public void AddBatch(MedicineBatch batch)
        => Db.Set<MedicineBatch>().Add(batch);

    public void RemoveBatch(MedicineBatch batch)
        => Db.Set<MedicineBatch>().Remove(batch);

    public void AddAdjustment(InventoryAdjustment adjustment)
        => Db.Set<InventoryAdjustment>().Add(adjustment);

    public async Task<bool> MedicineNameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        return await Db.Set<Medicine>()
            .AnyAsync(m => m.Name == trimmed && (excludeId == null || m.Id != excludeId), cancellationToken);
    }

    public async Task<bool> BatchNumberExistsAsync(string batchNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var trimmed = batchNumber.Trim();
        return await Db.Set<MedicineBatch>()
            .AnyAsync(b => b.BatchNumber == trimmed && (excludeId == null || b.Id != excludeId), cancellationToken);
    }

    public async Task<PagedList<MedicineListItemDto>> ListAsync(ListMedicinesQuery query, CancellationToken cancellationToken = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        // Use projection to fetch only required columns and compute aggregates in the database.
        IQueryable<Medicine> baseQuery = Db.Set<Medicine>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            // NOTE: This uses EF Core's LIKE semantics (Contains -> LIKE '%%').
            // If this table grows very large consider replacing with Full-Text Search or trigram indexes.
            baseQuery = baseQuery.Where(m =>
                m.Name.Contains(search) ||
                (m.GenericName != null && m.GenericName.Name.Contains(search)) ||
                m.CategoryEnum.ToString().Contains(search));
        }

        if (query.CategoryId.HasValue)
            baseQuery = baseQuery.Where(m => (int)m.CategoryEnum == query.CategoryId.Value);

        if (query.Form.HasValue)
            baseQuery = baseQuery.Where(m => m.Variants.Any(v => v.Form == query.Form.Value && !v.IsDeleted));

        if (query.IsActive.HasValue)
            baseQuery = baseQuery.Where(m => m.IsActive == query.IsActive.Value);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Project to a lightweight anonymous type; perform sums/counts in SQL.
        var projected = baseQuery.Select(m => new
        {
            m.Id,
            m.Name,
            m.NameAr,
            GenericName = m.GenericName != null ? m.GenericName.Name : string.Empty,
            GenericNameAr = m.GenericName != null ? m.GenericName.NameAr : (string?)null,
            Category = m.CategoryEnum,
            m.IsControlled,
            m.IsActive,
            ReorderLevel = m.ReorderLevel.Value,
            // Variant summaries
            Variants = m.Variants
                .Where(v => !v.IsDeleted && v.IsActive)
                .Select(v => new
                {
                    v.Id,
                    v.Form,
                    v.Unit,
                    v.Strength,
                    BaseUnitName = v.UnitOfMeasure.BaseUnitName,
                    PackageUnitName = v.UnitOfMeasure.PackageUnitName,
                    UnitsPerPackage = v.UnitOfMeasure.UnitsPerPackage,
                    IsDivisible = v.UnitOfMeasure.IsDivisible,
                    Available = v.Batches.Where(b => !b.IsDeleted && b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0
                })
                .OrderBy(v => v.Form)
                .ToList(),
            // AvailableQuantity intentionally omitted from SQL projection to avoid duplicate SUM computation;
            // it will be computed in-memory from the per-variant Available values after the query.
            VariantCount = m.Variants.Count(v => !v.IsDeleted && v.IsActive),
            FirstForm = m.Variants.Where(v => !v.IsDeleted).OrderBy(v => v.Form).Select(v => v.Form).FirstOrDefault(),
            CreatedAt = m.CreatedAt
        });

        // Apply sorting on the projected fields.
        var sortBy = query.SortBy?.ToLowerInvariant();
        projected = sortBy switch
        {
            "createdat" => SortDir(projected, x => x.CreatedAt, query.SortDir),
            "category" => SortDir(projected, x => x.Category, query.SortDir),
            "form" => SortDir(projected, x => x.FirstForm, query.SortDir),
            _ => SortDir(projected, x => x.Name, query.SortDir)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var rows = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(r =>
        {
            // compute AvailableQuantity in-memory from per-variant sums to avoid repeating SUM in SQL
            var availableQuantity = r.Variants.Sum(v => v.Available);
            var variants = r.Variants.Select(v => new MedicineVariantSummaryDto(
                v.Id,
                v.Form,
                v.Unit,
                v.Strength,
                $"{v.Form} {v.Strength} {v.Unit}",
                v.Available,
                v.BaseUnitName,
                v.PackageUnitName,
                v.UnitsPerPackage,
                v.IsDivisible)).ToList();

            return new MedicineListItemDto(
                r.Id,
                r.Name,
                r.NameAr,
                r.GenericName,
                r.GenericNameAr,
                r.Category,
                variants,
                r.IsControlled,
                r.IsActive,
                r.ReorderLevel,
                availableQuantity,
                r.VariantCount,
                availableQuantity <= r.ReorderLevel);
        }).ToList();

        return new PagedList<MedicineListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }


    public async Task<PagedList<MedicineBatchDto>> ListBatchesAsync(BatchListQuery query, CancellationToken cancellationToken = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<MedicineBatch> data = Db.Set<MedicineBatch>().AsNoTracking();

        if (query.MedicineId.HasValue)
            data = data.Where(b => b.MedicineVariant!.MedicineId == query.MedicineId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            data = data.Where(b => b.BatchNumber.Contains(query.Search.Trim()));

        switch (query.ExpiryStatus?.ToLowerInvariant())
        {
            case "valid":
                data = data.Where(b => b.ExpiryDate > asOf);
                break;
            case "expirings" or "expiringsoon":
                data = data.Where(b => b.ExpiryDate > asOf && b.ExpiryDate <= asOf.AddDays(query.WithinDays));
                break;
            case "expired":
                data = data.Where(b => b.ExpiryDate <= asOf);
                break;
        }

        var totalCount = await data.CountAsync(cancellationToken);

        data = query.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => SortDir(data, b => b.QuantityAvailable.Value, query.SortDir),
            "batch" => SortDir(data, b => b.BatchNumber, query.SortDir),
            _ => SortDir(data, b => b.ExpiryDate, query.SortDir)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var projected = data
            .Select(b => new
            {
                b.Id,
                b.MedicineVariant!.MedicineId,
                MedicineName = b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.Name : "Unknown",
                MedicineNameAr = b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.NameAr : (string?)null,
                VariantName = $"{b.MedicineVariant!.Form} {b.MedicineVariant!.Strength} {b.MedicineVariant!.Unit}",
                b.BatchNumber,
                b.ManufactureDate,
                b.ExpiryDate,
                QuantityReceived = b.QuantityReceived.Value,
                QuantityAvailable = b.QuantityAvailable.Value,
                Dispensed = Db.Set<DispensingRecordItem>().Where(i => i.MedicineBatchId == b.Id).Sum(i => (int?)i.Quantity.Value) ?? 0,
                UnitCost = b.UnitCost.Amount,
                b.SupplierName,
                IsExpired = b.ExpiryDate <= asOf,
                DaysToExpiry = b.ExpiryDate.DayNumber - asOf.DayNumber,
                BatchStatus = b.ExpiryDate <= asOf ? "Expired" : (b.QuantityAvailable.Value <= 0 ? "Depleted" : "Active"),
                b.CreatedAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await projected.ToListAsync(cancellationToken);

        return new PagedList<MedicineBatchDto>
        {
            Items = rows.Select(r => new MedicineBatchDto(
                r.Id,
                r.MedicineId,
                r.MedicineName,
                r.MedicineNameAr,
                r.VariantName,
                r.BatchNumber,
                r.ManufactureDate,
                r.ExpiryDate,
                r.QuantityReceived,
                r.QuantityAvailable,
                r.Dispensed,
                r.UnitCost,
                r.SupplierName,
                r.IsExpired,
                r.DaysToExpiry,
                r.BatchStatus,
                r.CreatedAt)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedList<MedicineInventorySummaryDto>> ListMedicineSummaryAsync(
        MedicineInventorySummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<Medicine> data = Db.Set<Medicine>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            // NOTE: Uses SQL LIKE via Contains ("%search%"). Consider Full-Text Search or trigram indexes for large medicine catalogs.
            data = data.Where(m =>
                m.Name.Contains(search) ||
                (m.GenericName != null && m.GenericName.Name.Contains(search)));
        }

        var projected = data.Select(m => new
        {
            m.Id,
            m.Name,
            NameAr = m.NameAr,
            GenericName = m.GenericName != null ? m.GenericName.Name : string.Empty,
            GenericNameAr = m.GenericName != null ? m.GenericName.NameAr : null,
            ReorderLevel = m.ReorderLevel.Value,
            TotalQuantity = m.Variants
                .Where(v => v.IsActive)
                .SelectMany(v => v.Batches.Where(b => b.ExpiryDate > asOf))
                .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
            VariantCount = m.Variants.Count(v => v.IsActive),
            ActiveBatchCount = m.Variants
                .SelectMany(v => v.Batches)
                .Count(b => b.ExpiryDate > asOf && b.QuantityAvailable.Value > 0),
            NearestExpiryDate = m.Variants
                .SelectMany(v => v.Batches)
                .Where(b => b.ExpiryDate >= asOf)
                .Min(b => (DateOnly?)b.ExpiryDate)
        });

        switch (query.StockStatus?.ToLowerInvariant())
        {
            case "instock" or "in_stock":
                projected = projected.Where(x => x.TotalQuantity > x.ReorderLevel);
                break;
            case "low" or "lowstock" or "low_stock":
                projected = projected.Where(x => x.TotalQuantity > 0 && x.TotalQuantity <= x.ReorderLevel);
                break;
            case "out" or "outofstock" or "out_of_stock":
                projected = projected.Where(x => x.TotalQuantity == 0);
                break;
        }

        var totalCount = await projected.CountAsync(cancellationToken);

        projected = query.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" or "totalquantity" => SortDir(projected, x => x.TotalQuantity, query.SortDir),
            "reorder" or "reorderlevel" => SortDir(projected, x => x.ReorderLevel, query.SortDir),
            "nearestExpiry" or "expiry" => SortDir(projected, x => x.NearestExpiryDate, query.SortDir),
            "variantCount" or "variants" => SortDir(projected, x => x.VariantCount, query.SortDir),
            _ => SortDir(projected, x => x.Name, query.SortDir)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var rows = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new MedicineInventorySummaryDto(
                r.Id,
                r.Name,
                r.NameAr,
                r.GenericName,
                r.GenericNameAr,
                r.VariantCount,
                r.TotalQuantity,
                r.ReorderLevel,
                r.TotalQuantity == 0
                    ? InventoryStatus.OutOfStock
                    : r.TotalQuantity <= r.ReorderLevel
                        ? InventoryStatus.LowStock
                        : InventoryStatus.InStock,
                r.NearestExpiryDate,
                r.ActiveBatchCount))
            .ToList();

        return new PagedList<MedicineInventorySummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedList<ExpiryAlertDto>> ListExpiryAlertsAsync(
        ExpiryAlertListQuery query,
        CancellationToken cancellationToken = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<MedicineBatch> data = Db.Set<MedicineBatch>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            // NOTE: Uses SQL LIKE via Contains ("%search%"). For large batch tables consider full-text search or trigram indexes on batch number/medicine name.
            data = data.Where(b =>
                b.BatchNumber.Contains(search) ||
                (b.MedicineVariant != null && b.MedicineVariant.Medicine != null && b.MedicineVariant.Medicine.Name.Contains(search)));
        }

        switch (query.Status?.ToLowerInvariant())
        {
            case "expired":
                data = data.Where(b => b.ExpiryDate < asOf);
                break;
            case "critical":
                data = data.Where(b => b.ExpiryDate >= asOf && b.ExpiryDate < asOf.AddDays(30));
                break;
            case "warning":
                data = data.Where(b => b.ExpiryDate >= asOf.AddDays(30) && b.ExpiryDate < asOf.AddDays(90));
                break;
            case "safe":
                data = data.Where(b => b.ExpiryDate >= asOf.AddDays(90));
                break;
        }

        var totalCount = await data.CountAsync(cancellationToken);

        var projected = data.Select(b => new
        {
            b.Id,
            b.BatchNumber,
            b.ExpiryDate,
            Remaining = b.QuantityAvailable.Value,
            MedicineName = b.MedicineVariant != null && b.MedicineVariant.Medicine != null
                ? b.MedicineVariant.Medicine.Name : "Unknown",
            MedicineNameAr = b.MedicineVariant != null && b.MedicineVariant.Medicine != null
                ? b.MedicineVariant.Medicine.NameAr : null,
            Form = b.MedicineVariant != null ? b.MedicineVariant.Form : (MedicineForm?)null,
            Unit = b.MedicineVariant != null ? b.MedicineVariant.Unit : (MedicineUnit?)null,
            Strength = b.MedicineVariant != null ? b.MedicineVariant.Strength : (decimal?)null,
            DaysToExpiry = EF.Functions.DateDiffDay(asOf, b.ExpiryDate)
        });

        projected = query.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "remaining" => SortDir(projected, x => x.Remaining, query.SortDir),
            "batch" or "batchnumber" => SortDir(projected, x => x.BatchNumber, query.SortDir),
            "daysToExpiry" or "days" => SortDir(projected, x => x.DaysToExpiry, query.SortDir),
            _ => SortDir(projected, x => x.ExpiryDate, query.SortDir)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var rows = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r =>
            {
                var status = r.DaysToExpiry < 0
                    ? ExpiryStatus.Expired
                    : r.DaysToExpiry < 30
                        ? ExpiryStatus.Critical
                        : r.DaysToExpiry < 90
                            ? ExpiryStatus.Warning
                            : ExpiryStatus.Safe;
                var variantName = r.Strength is null
                    ? $"{r.Form} {r.Unit}".Trim()
                    : $"{r.Form} {r.Strength} {r.Unit}";
                return new ExpiryAlertDto(
                    r.Id, r.MedicineName, r.MedicineNameAr, variantName, r.BatchNumber,
                    r.ExpiryDate, r.DaysToExpiry, r.Remaining, status);
            })
            .ToList();

        return new PagedList<ExpiryAlertDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<LowStockDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var rows = await Db.Set<Medicine>()
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new
            {
                m.Id,
                m.Name,
                NameAr = m.NameAr,
                ReorderLevel = m.ReorderLevel.Value,
                Available = m.Variants
                    .Where(v => v.IsActive)
                    .SelectMany(v => v.Batches.Where(b => b.ExpiryDate > asOf))
                    .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0
            })
            .Where(x => x.Available <= x.ReorderLevel)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new LowStockDto(r.Id, r.Name, r.NameAr, r.Available, r.ReorderLevel))
            .ToList();
    }

    public async Task<PagedList<InventoryAdjustmentDto>> ListAdjustmentsAsync(InventoryAdjustmentListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<InventoryAdjustment> data = Db.Set<InventoryAdjustment>().AsNoTracking();

        if (query.Type.HasValue)
            data = data.Where(a => a.Type == query.Type.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            data = data.Where(a => a.Reason.Contains(query.Search.Trim()));

        var totalCount = await data.CountAsync(cancellationToken);

        data = query.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => SortDir(data, a => a.QuantityChanged, query.SortDir),
            _ => SortDir(data, a => a.AdjustedAt, query.SortDir)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var rows = await data
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.MedicineBatchId,
                a.Type,
                a.QuantityChanged,
                a.QuantityBefore,
                a.QuantityAfter,
                a.Reason,
                a.AdjustedAt,
                a.AdjustedBy,
                MedicineName = a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null && a.MedicineBatch.MedicineVariant.Medicine != null
                    ? a.MedicineBatch.MedicineVariant.Medicine.Name : "Unknown",
                MedicineNameAr = a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null && a.MedicineBatch.MedicineVariant.Medicine != null
                    ? a.MedicineBatch.MedicineVariant.Medicine.NameAr : null,
                BatchNumber = a.MedicineBatch != null ? a.MedicineBatch.BatchNumber : string.Empty,
                Form = a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null ? a.MedicineBatch.MedicineVariant.Form : (MedicineForm?)null,
                Unit = a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null ? a.MedicineBatch.MedicineVariant.Unit : (MedicineUnit?)null,
                Strength = a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null ? a.MedicineBatch.MedicineVariant.Strength : (decimal?)null
            })
            .ToListAsync(cancellationToken);

        var adjustedByIds = rows.Where(r => r.AdjustedBy.HasValue).Select(r => r.AdjustedBy!.Value).Distinct().ToList();
        var userNames = adjustedByIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await Db.Set<ApplicationUser>().AsNoTracking()
                .Where(u => adjustedByIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim(), cancellationToken);

        var items = rows
            .Select(r => new InventoryAdjustmentDto(
                r.Id,
                r.MedicineBatchId,
                r.MedicineName,
                r.MedicineNameAr,
                r.Strength is null ? $"{r.Form} {r.Unit}".Trim() : $"{r.Form} {r.Strength} {r.Unit}",
                r.BatchNumber,
                r.Type.ToString(),
                r.QuantityChanged,
                r.QuantityBefore,
                r.QuantityAfter,
                r.Reason,
                r.AdjustedBy,
                r.AdjustedBy.HasValue ? userNames.GetValueOrDefault(r.AdjustedBy.Value) : null,
                r.AdjustedAt))
            .ToList();

        return new PagedList<InventoryAdjustmentDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}