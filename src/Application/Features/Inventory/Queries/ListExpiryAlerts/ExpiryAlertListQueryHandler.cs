using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ExpiryAlertListQueryHandler : IRequestHandler<ExpiryAlertListQuery, Result<PagedList<ExpiryAlertDto>>>
{
    private const int CriticalWithinDays = 30;
    private const int WarningWithinDays = 90;

    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;

    public ExpiryAlertListQueryHandler(IMedicineRepository repo, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _executor = executor;
    }

    public async Task<Result<PagedList<ExpiryAlertDto>>> Handle(ExpiryAlertListQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var batches = _repo.Query()
            .SelectMany(m => m.Variants.SelectMany(v => v.Batches.Select(b => new
            {
                Batch = b,
                MedicineName = m.Name,
                MedicineNameAr = m.NameAr,
                Variant = v
            })));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            batches = batches.Where(x =>
                x.Batch.BatchNumber.Contains(search) ||
                x.MedicineName.Contains(search));
        }

        switch (request.Status?.ToLowerInvariant())
        {
            case "expired":
                batches = batches.Where(x => x.Batch.ExpiryDate < asOf);
                break;
            case "critical":
                batches = batches.Where(x => x.Batch.ExpiryDate >= asOf && x.Batch.ExpiryDate < asOf.AddDays(CriticalWithinDays));
                break;
            case "warning":
                batches = batches.Where(x => x.Batch.ExpiryDate >= asOf.AddDays(CriticalWithinDays) && x.Batch.ExpiryDate < asOf.AddDays(WarningWithinDays));
                break;
            case "safe":
                batches = batches.Where(x => x.Batch.ExpiryDate >= asOf.AddDays(WarningWithinDays));
                break;
        }

        // NOTE: sorting must use entity members BEFORE the row projection —
        // EF cannot translate member access on a constructed record.
        // Days-to-expiry ordering == expiry-date ordering (asOf is constant).
        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "remaining" => SortDir(batches, x => x.Batch.QuantityAvailable.Value, request.SortDir),
            "batch" or "batchnumber" => SortDir(batches, x => x.Batch.BatchNumber, request.SortDir),
            _ => SortDir(batches, x => x.Batch.ExpiryDate, request.SortDir)
        };

        var totalCount = await _executor.CountAsync(sorted, cancellationToken);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var rows = await _executor.ToListAsync(
            sorted.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new ExpiryAlertRow(
                    x.Batch.Id,
                    x.MedicineName,
                    x.MedicineNameAr,
                    (MedicineForm?)x.Variant.Form,
                    (MedicineUnit?)x.Variant.Unit,
                    (decimal?)x.Variant.Strength,
                    x.Batch.BatchNumber,
                    x.Batch.ExpiryDate,
                    x.Batch.ExpiryDate.DayNumber - asOf.DayNumber,
                    x.Batch.QuantityAvailable.Value)),
            cancellationToken);

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<ExpiryAlertDto>>.Success(items);
    }

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
