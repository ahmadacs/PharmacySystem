using System.Linq.Expressions;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ListLowStockQueryHandler : IRequestHandler<ListLowStockQuery, Result<PagedList<LowStockDto>>>
{
    private readonly IBaseRepository<MedicineVariant> _repo;

    public ListLowStockQueryHandler(IBaseRepository<MedicineVariant> repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<LowStockDto>>> Handle(ListLowStockQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        Expression<Func<MedicineVariant, bool>> predicate =
            v => v.IsActive && v.Medicine!.IsActive
                && v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value;

        var paged = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" => await _repo.PagedAsync(LowStockProjection(asOf), predicate, v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0, request.ToPagination(), cancellationToken),
            "strength" => await _repo.PagedAsync(LowStockProjection(asOf), predicate, v => v.Strength, request.ToPagination(), cancellationToken),
            _ => await _repo.PagedAsync(LowStockProjection(asOf), predicate, v => v.Medicine!.Name, request.ToPagination(), cancellationToken)
        };

        var items = paged.Select(r => r.ToDto());

        return Result<PagedList<LowStockDto>>.Success(items);
    }

    private static Expression<Func<MedicineVariant, LowStockRow>> LowStockProjection(DateOnly asOf) => v => new LowStockRow(
        v.MedicineId,
        v.Medicine!.Name,
        v.Medicine!.NameAr,
        v.Id,
        v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
        v.ReorderLevel.Value,
        v.Form,
        v.Unit,
        v.Strength);
}
