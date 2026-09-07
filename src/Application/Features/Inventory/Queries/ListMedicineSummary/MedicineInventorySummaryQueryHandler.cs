using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class MedicineInventorySummaryQueryHandler
    : IRequestHandler<MedicineInventorySummaryQuery, PagedList<MedicineInventorySummaryDto>>
{
    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;

    public MedicineInventorySummaryQueryHandler(IMedicineRepository repo, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _executor = executor;
    }

    public async Task<PagedList<MedicineInventorySummaryDto>> Handle(
        MedicineInventorySummaryQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<Medicine> data = _repo.Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            data = data.Where(m =>
                m.Name.Contains(search) ||
                (m.NameAr != null && m.NameAr.Contains(search)) ||
                (m.GenericName != null && m.GenericName.Name.Contains(search)) ||
                (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)));
        }

        // NOTE: filtering/sorting must use entity members BEFORE the row
        // projection — EF cannot translate member access on a constructed
        // record (e.g. Where(x => x.TotalQuantity > 0) fails). The aggregate
        // expressions below are passed directly (never invoked) so they stay
        // translatable; asOf is captured as a query constant.
        data = request.StockStatus?.ToLowerInvariant() switch
        {
            "instock" or "in_stock" => data.Where(InStock(asOf)),
            "low" or "lowstock" or "low_stock" => data.Where(LowStock(asOf)),
            "out" or "outofstock" or "out_of_stock" => data.Where(OutOfStock(asOf)),
            _ => data
        };

        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" or "totalquantity" => SortDir(data, TotalQuantity(asOf), request.SortDir),
            "reorder" or "reorderlevel" => SortDir(data, ReorderLevelSum(), request.SortDir),
            "nearestExpiry" or "expiry" => SortDir(data, NearestExpiry(asOf), request.SortDir),
            "variantCount" or "variants" => SortDir(data, m => m.Variants.Count(v => v.IsActive && !v.IsDeleted), request.SortDir),
            _ => SortDir(data, m => m.Name, request.SortDir)
        };

        var totalCount = await _executor.CountAsync(sorted, cancellationToken);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var rows = await _executor.ToListAsync(
            sorted.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new MedicineInventorySummaryRow(
                    m.Id,
                    m.Name,
                    m.NameAr,
                    m.GenericName != null ? m.GenericName.Name : string.Empty,
                    m.GenericName != null ? m.GenericName.NameAr : null,
                    m.Variants.Count(v => v.IsActive && !v.IsDeleted),
                    m.Variants
                        .Where(v => v.IsActive && !v.IsDeleted)
                        .SelectMany(v => v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted))
                        .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                    m.Variants.Where(v => v.IsActive && !v.IsDeleted).Sum(v => (int?)v.ReorderLevel.Value) ?? 0,
                    m.Variants.Where(v => v.IsActive && !v.IsDeleted).Any(v =>
                        v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value),
                    m.Variants
                        .SelectMany(v => v.Batches)
                        .Where(b => b.ExpiryDate >= asOf && !b.IsDeleted)
                        .Min(b => (DateOnly?)b.ExpiryDate),
                    m.Variants
                        .SelectMany(v => v.Batches)
                        .Count(b => b.ExpiryDate > asOf && b.QuantityAvailable.Value > 0 && !b.IsDeleted))),
            cancellationToken);

        return rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);
    }

    private static Expression<Func<Medicine, int>> TotalQuantity(DateOnly asOf)
        => m => m.Variants
            .Where(v => v.IsActive && !v.IsDeleted)
            .SelectMany(v => v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted))
            .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0;

    private static Expression<Func<Medicine, int>> ReorderLevelSum()
        => m => m.Variants.Where(v => v.IsActive && !v.IsDeleted).Sum(v => (int?)v.ReorderLevel.Value) ?? 0;

    private static Expression<Func<Medicine, DateOnly?>> NearestExpiry(DateOnly asOf)
        => m => m.Variants
            .SelectMany(v => v.Batches)
            .Where(b => b.ExpiryDate >= asOf && !b.IsDeleted)
            .Min(b => (DateOnly?)b.ExpiryDate);

    private static Expression<Func<Medicine, bool>> InStock(DateOnly asOf)
        => m => (m.Variants
                .Where(v => v.IsActive && !v.IsDeleted)
                .SelectMany(v => v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted))
                .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0) > 0
            && !m.Variants.Where(v => v.IsActive && !v.IsDeleted).Any(v =>
                v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value);

    private static Expression<Func<Medicine, bool>> LowStock(DateOnly asOf)
        => m => (m.Variants
                .Where(v => v.IsActive && !v.IsDeleted)
                .SelectMany(v => v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted))
                .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0) > 0
            && m.Variants.Where(v => v.IsActive && !v.IsDeleted).Any(v =>
                v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value);

    private static Expression<Func<Medicine, bool>> OutOfStock(DateOnly asOf)
        => m => (m.Variants
                .Where(v => v.IsActive && !v.IsDeleted)
                .SelectMany(v => v.Batches.Where(b => b.ExpiryDate > asOf && !b.IsDeleted))
                .Sum(b => (int?)b.QuantityAvailable.Value) ?? 0) == 0;

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
