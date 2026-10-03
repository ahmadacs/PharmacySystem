using System.Linq.Expressions;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class MedicineInventorySummaryQueryHandler
    : IRequestHandler<MedicineInventorySummaryQuery, Result<PagedList<MedicineInventorySummaryDto>>>
{
    private readonly IRepository<Medicine> _repo;

    public MedicineInventorySummaryQueryHandler(IRepository<Medicine> repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<MedicineInventorySummaryDto>>> Handle(
        MedicineInventorySummaryQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var stockStatus = request.StockStatus?.ToLowerInvariant() switch
        {
            "instock" or "in_stock" => "instock",
            "low" or "lowstock" or "low_stock" => "low",
            "out" or "outofstock" or "out_of_stock" => "out",
            _ => null
        };

        Expression<Func<Medicine, bool>> predicate = stockStatus switch
        {
            "instock" => m => (search == null
                    || m.Name.Contains(search)
                    || (m.NameAr != null && m.NameAr.Contains(search))
                    || (m.GenericName != null && m.GenericName.Name.Contains(search))
                    || (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)))
                && (m.Variants.Where(v => v.IsActive).Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0) > 0
                && !m.Variants.Where(v => v.IsActive).Any(v =>
                    v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value),
            "low" => m => (search == null
                    || m.Name.Contains(search)
                    || (m.NameAr != null && m.NameAr.Contains(search))
                    || (m.GenericName != null && m.GenericName.Name.Contains(search))
                    || (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)))
                && (m.Variants.Where(v => v.IsActive).Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0) > 0
                && m.Variants.Where(v => v.IsActive).Any(v =>
                    v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value),
            "out" => m => (search == null
                    || m.Name.Contains(search)
                    || (m.NameAr != null && m.NameAr.Contains(search))
                    || (m.GenericName != null && m.GenericName.Name.Contains(search))
                    || (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)))
                && (m.Variants.Where(v => v.IsActive).Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0) == 0,
            _ => m => search == null
                || m.Name.Contains(search)
                || (m.NameAr != null && m.NameAr.Contains(search))
                || (m.GenericName != null && m.GenericName.Name.Contains(search))
                || (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)),
        };

        var paged = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" or "totalquantity" => await _repo.PagedAsync(SummaryRowProjection(asOf), predicate, m => m.Variants.Where(v => v.IsActive).Sum(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value)) ?? 0, request.ToPagination(), cancellationToken),
            "reorder" or "reorderlevel" => await _repo.PagedAsync(SummaryRowProjection(asOf), predicate, m => m.Variants.Where(v => v.IsActive).Sum(v => (int?)v.ReorderLevel.Value) ?? 0, request.ToPagination(), cancellationToken),
            "nearestExpiry" or "expiry" => await _repo.PagedAsync(SummaryRowProjection(asOf), predicate, m => m.Variants.Min(v => v.Batches.Where(b => b.ExpiryDate >= asOf).Min(b => (DateOnly?)b.ExpiryDate)), request.ToPagination(), cancellationToken),
            "variantCount" or "variants" => await _repo.PagedAsync(SummaryRowProjection(asOf), predicate, m => m.Variants.Count(v => v.IsActive), request.ToPagination(), cancellationToken),
            _ => await _repo.PagedAsync(SummaryRowProjection(asOf), predicate, m => m.Name, request.ToPagination(), cancellationToken)
        };

        var items = paged.Select(r => r.ToDto());

        return Result<PagedList<MedicineInventorySummaryDto>>.Success(items);
    }

    private static Expression<Func<Medicine, MedicineInventorySummaryRow>> SummaryRowProjection(DateOnly asOf) => m => new MedicineInventorySummaryRow(
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
            .Sum(v => (int?)v.Batches.Count(b => b.ExpiryDate > asOf && b.QuantityAvailable.Value > 0)) ?? 0);
}
