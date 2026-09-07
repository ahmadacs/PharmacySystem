using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class BatchListQueryHandler : IRequestHandler<BatchListQuery, Result<PagedList<MedicineBatchDto>>>
{
    private readonly IMedicineRepository _repo;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IAsyncQueryExecutor _executor;

    public BatchListQueryHandler(IMedicineRepository repo, IPrescriptionRepository prescriptions, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _prescriptions = prescriptions;
        _executor = executor;
    }

    public async Task<Result<PagedList<MedicineBatchDto>>> Handle(BatchListQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var (expiryFrom, expiryTo) = GetExpiryRange(request.ExpiryStatus, asOf, request.WithinDays);

        IQueryable<MedicineBatch> filtered = _repo.QueryBatches();

        if (request.MedicineId.HasValue)
            filtered = filtered.Where(b => b.MedicineVariant!.MedicineId == request.MedicineId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            filtered = filtered.Where(b => b.BatchNumber.Contains(request.Search.Trim()));

        if (expiryFrom.HasValue)
            filtered = filtered.Where(b => b.ExpiryDate > expiryFrom.Value);

        if (expiryTo.HasValue)
            filtered = filtered.Where(b => b.ExpiryDate <= expiryTo.Value);

        var ordered = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => SortDir(filtered, b => b.QuantityAvailable.Value, request.SortDir),
            "batch" => SortDir(filtered, b => b.BatchNumber, request.SortDir),
            _ => SortDir(filtered, b => b.ExpiryDate, request.SortDir)
        };

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var dispensingLines = _prescriptions.QueryDispensingRecordItems();

        // ONE round trip for rows: dispensed-per-batch rides as a correlated
        // subquery. TotalCount stays a separate query so it is exact even on
        // overflow pages.
        var rows = await _executor.ToListAsync(
            ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new MedicineBatchRow(
                    b.Id,
                    b.MedicineVariant!.MedicineId,
                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.Name : "Unknown",
                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.NameAr : null,
                    $"{b.MedicineVariant!.Form} {b.MedicineVariant!.Strength} {b.MedicineVariant!.Unit}",
                    b.BatchNumber,
                    b.ManufactureDate,
                    b.ExpiryDate,
                    b.QuantityReceived.Value,
                    b.QuantityAvailable.Value,
                    b.UnitCost.Amount,
                    b.SupplierName,
                    b.CreatedAt,
                    dispensingLines
                        .Where(i => i.MedicineBatchId == b.Id)
                        .Sum(i => (int?)i.Quantity.Value) ?? 0)),
            cancellationToken);

        var totalCount = await _executor.CountAsync(filtered, cancellationToken);

        var items = rows
            .Select(r => r.ToDto(asOf))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<MedicineBatchDto>>.Success(items);
    }

    private static (DateOnly? From, DateOnly? To) GetExpiryRange(string? expiryStatus, DateOnly asOf, int withinDays)
        => expiryStatus?.ToLowerInvariant() switch
        {
            "valid" => (asOf, null),
            "expirings" or "expiringsoon" => (asOf, asOf.AddDays(withinDays)),
            "expired" => (null, asOf),
            _ => (null, null)
        };

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
