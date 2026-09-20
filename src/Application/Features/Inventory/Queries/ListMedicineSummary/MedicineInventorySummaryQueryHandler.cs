using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class MedicineInventorySummaryQueryHandler
    : IRequestHandler<MedicineInventorySummaryQuery, Result<PagedList<MedicineInventorySummaryDto>>>
{
    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;

    public MedicineInventorySummaryQueryHandler(IMedicineRepository repo, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _executor = executor;
    }

    public async Task<Result<PagedList<MedicineInventorySummaryDto>>> Handle(
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

        data = request.StockStatus?.ToLowerInvariant() switch
        {
            "instock" or "in_stock" => data.Where(HasStock(asOf)).Where(HasNoLowVariant(asOf)),
            "low" or "lowstock" or "low_stock" => data.Where(HasStock(asOf)).Where(HasLowVariant(asOf)),
            "out" or "outofstock" or "out_of_stock" => data.Where(HasNoStock(asOf)),
            _ => data
        };

        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" or "totalquantity" => SortDir(data, TotalQuantity(asOf), request.SortDir),
            "reorder" or "reorderlevel" => SortDir(data, ReorderLevelSum(), request.SortDir),
            "nearestExpiry" or "expiry" => SortDir(data, NearestExpiry(asOf), request.SortDir),
            "variantCount" or "variants" => SortDir(data, m => m.Variants.Count(v => v.IsActive), request.SortDir),
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
                    m.Variants.Count(v => v.IsActive),
                    m.Variants
                        .Where(v => v.IsActive)
                        .Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0,
                    m.Variants.Where(v => v.IsActive).Sum(v => (int?)v.ReorderLevel.Value) ?? 0,
                    m.Variants.Where(v => v.IsActive).Any(v =>
                        v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value),
                    m.Variants
                        .Min(v => v.Batches.Where(b => b.ExpiryDate >= asOf).Min(b => (DateOnly?)b.ExpiryDate)),
                    m.Variants
                        .Sum(v => (int?)v.Batches.Count(b => b.ExpiryDate > asOf && b.QuantityAvailable.Value > 0)) ?? 0)),
            cancellationToken);

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<MedicineInventorySummaryDto>>.Success(items);
    }

    private static Expression<Func<Medicine, int>> TotalQuantity(DateOnly asOf)
        => m => m.Variants
            .Where(v => v.IsActive)
            .Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0;

    private static Expression<Func<Medicine, int>> ReorderLevelSum()
        => m => m.Variants.Where(v => v.IsActive).Sum(v => (int?)v.ReorderLevel.Value) ?? 0;

    private static Expression<Func<Medicine, DateOnly?>> NearestExpiry(DateOnly asOf)
        => m => m.Variants
            .Min(v => v.Batches.Where(b => b.ExpiryDate >= asOf).Min(b => (DateOnly?)b.ExpiryDate));

    private static Expression<Func<Medicine, bool>> HasStock(DateOnly asOf)
        => m => (m.Variants
                .Where(v => v.IsActive)
                .Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0) > 0;

    private static Expression<Func<Medicine, bool>> HasNoStock(DateOnly asOf)
        => m => (m.Variants
                .Where(v => v.IsActive)
                .Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0) == 0;

    private static Expression<Func<Medicine, bool>> HasLowVariant(DateOnly asOf)
        => m => m.Variants.Where(v => v.IsActive).Any(v =>
                v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value);

    private static Expression<Func<Medicine, bool>> HasNoLowVariant(DateOnly asOf)
        => m => !m.Variants.Where(v => v.IsActive).Any(v =>
                v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value);

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
