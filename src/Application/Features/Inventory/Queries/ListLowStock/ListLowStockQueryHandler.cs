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


        var selector = (Expression<Func<MedicineVariant, LowStockRow>>)(v => new LowStockRow(
                    v.MedicineId,
                    v.Medicine!.Name,
                    v.Medicine!.NameAr,
                    v.Id,
                    v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                    v.ReorderLevel.Value,
                    v.Form,
                    v.Unit,
                    v.Strength));

        Expression<Func<MedicineVariant, bool>> predicate =
            v => v.IsActive && v.Medicine!.IsActive
                && v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value;

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);
        var desc = request.SortDir.IsDescending();

        var totalCount = await _repo.CountAsync(predicate, cancellationToken);
        List<LowStockRow> rows = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "available" => await _repo.PagedAsync(selector, predicate, v => v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0, desc, page, pageSize, cancellationToken),
            "strength" => await _repo.PagedAsync(selector, predicate, v => v.Strength, desc, page, pageSize, cancellationToken),
            _ => await _repo.PagedAsync(selector, predicate, v => v.Medicine!.Name, desc, page, pageSize, cancellationToken)
        };

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<LowStockDto>>.Success(items);
    }
}
