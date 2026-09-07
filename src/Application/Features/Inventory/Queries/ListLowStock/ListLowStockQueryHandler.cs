using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ListLowStockQueryHandler : IRequestHandler<ListLowStockQuery, Result<IReadOnlyList<LowStockDto>>>
{
    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;

    public ListLowStockQueryHandler(IMedicineRepository repo, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<LowStockDto>>> Handle(ListLowStockQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        // NOTE: filtering/sorting must use entity members BEFORE the row
        // projection — EF cannot translate member access on a constructed
        // record (e.g. OrderBy(x => x.MedicineName) fails).
        var rows = await _executor.ToListAsync(
            _repo.Query()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .SelectMany(m => m.Variants
                    .Where(v => v.IsActive && !v.IsDeleted)
                    .Where(v => v.Batches.Where(b => !b.IsDeleted && b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) <= v.ReorderLevel.Value)
                    .OrderBy(v => v.Strength)
                    .Select(v => new LowStockRow(
                        m.Id,
                        m.Name,
                        m.NameAr,
                        v.Id,
                        v.Batches.Where(b => !b.IsDeleted && b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                        v.ReorderLevel.Value,
                        v.Form,
                        v.Unit,
                        v.Strength))),
            cancellationToken);

        return Result<IReadOnlyList<LowStockDto>>.Success(rows.Select(r => r.ToDto()).ToList());
    }
}
