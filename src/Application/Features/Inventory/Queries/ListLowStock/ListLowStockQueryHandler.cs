using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
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

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var spec = new Specification<MedicineVariant, LowStockRow>(v => new LowStockRow(
                    v.MedicineId,
                    v.Medicine!.Name,
                    v.Medicine!.NameAr,
                    v.Id,
                    v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                    v.ReorderLevel.Value,
                    v.Form,
                    v.Unit,
                    v.Strength));
        spec.Where(v => v.IsActive && v.Medicine!.IsActive);
        spec.Where(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value);

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" => SortDir(AvailableStock(asOf), request.SortDir),
            "strength" => SortDir(v => v.Strength, request.SortDir),
            _ => SortDir(v => v.Medicine!.Name, request.SortDir)
        });

        var totalCount = await _repo.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _repo.ListAsync(spec, cancellationToken);

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<LowStockDto>>.Success(items);
    }

    private static Expression<Func<MedicineVariant, int>> AvailableStock(DateOnly asOf)
        => v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0;

    private static Func<IQueryable<MedicineVariant>, IOrderedQueryable<MedicineVariant>> SortDir<TKey>(
        Expression<Func<MedicineVariant, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q => q.OrderByDescending(keySelector)
            : q => q.OrderBy(keySelector);
}
