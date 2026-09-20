using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ListLowStockQueryHandler : IRequestHandler<ListLowStockQuery, Result<PagedList<LowStockDto>>>
{
    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;

    public ListLowStockQueryHandler(IMedicineRepository repo, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _executor = executor;
    }

    public async Task<Result<PagedList<LowStockDto>>> Handle(ListLowStockQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<MedicineVariant> data = _repo.QueryVariants()
            .Where(v => v.IsActive && v.Medicine!.IsActive)
            .Where(v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value);

        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" => SortDir(data, AvailableStock(asOf), request.SortDir),
            "strength" => SortDir(data, v => v.Strength, request.SortDir),
            _ => SortDir(data, v => v.Medicine!.Name, request.SortDir)
        };

        var totalCount = await _executor.CountAsync(sorted, cancellationToken);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var rows = await _executor.ToListAsync(
            sorted.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(v => new LowStockRow(
                    v.MedicineId,
                    v.Medicine!.Name,
                    v.Medicine!.NameAr,
                    v.Id,
                    v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                    v.ReorderLevel.Value,
                    v.Form,
                    v.Unit,
                    v.Strength)),
            cancellationToken);

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<LowStockDto>>.Success(items);
    }

    private static Expression<Func<MedicineVariant, int>> AvailableStock(DateOnly asOf)
        => v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0;

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
