using System.Linq.Expressions;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class MedicineInventorySummaryQueryHandler
    : IRequestHandler<MedicineInventorySummaryQuery, Result<PagedList<MedicineInventorySummaryDto>>>
{
    private readonly IBaseRepository<Medicine> _repo;

    public MedicineInventorySummaryQueryHandler(IBaseRepository<Medicine> repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<MedicineInventorySummaryDto>>> Handle(
        MedicineInventorySummaryQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var spec = new Specification<Medicine, MedicineInventorySummaryRow>(m => new MedicineInventorySummaryRow(
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
                        .Sum(v => (int?)v.Batches.Count(b => b.ExpiryDate > asOf && b.QuantityAvailable.Value > 0)) ?? 0));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            spec.Where(m =>
                m.Name.Contains(search) ||
                (m.NameAr != null && m.NameAr.Contains(search)) ||
                (m.GenericName != null && m.GenericName.Name.Contains(search)) ||
                (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)));
        }

        switch (request.StockStatus?.ToLowerInvariant())
        {
            case "instock" or "in_stock":
                spec.Where(HasStock(asOf)).Where(HasNoLowVariant(asOf));
                break;
            case "low" or "lowstock" or "low_stock":
                spec.Where(HasStock(asOf)).Where(HasLowVariant(asOf));
                break;
            case "out" or "outofstock" or "out_of_stock":
                spec.Where(HasNoStock(asOf));
                break;
        }

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" or "totalquantity" => q => q.OrderByDirection(TotalQuantity(asOf), request.SortDir),
            "reorder" or "reorderlevel" => q => q.OrderByDirection(ReorderLevelSum(), request.SortDir),
            "nearestExpiry" or "expiry" => q => q.OrderByDirection(NearestExpiry(asOf), request.SortDir),
            "variantCount" or "variants" => q => q.OrderByDirection(m => m.Variants.Count(v => v.IsActive), request.SortDir),
            _ => q => q.OrderByDirection(m => m.Name, request.SortDir)
        });

        var totalCount = await _repo.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _repo.ListAsync(spec, cancellationToken);

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

}
