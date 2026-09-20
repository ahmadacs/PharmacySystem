using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
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
        var (expiryFrom, expiryTo) = GetExpiryRange(request.Status, asOf);

        IQueryable<MedicineBatch> data = _repo.QueryBatches();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            data = data.Where(b =>
                b.BatchNumber.Contains(search) ||
                b.MedicineVariant!.Medicine!.Name.Contains(search));
        }

        if (expiryFrom.HasValue)
            data = data.Where(b => b.ExpiryDate >= expiryFrom.Value);

        if (expiryTo.HasValue)
            data = data.Where(b => b.ExpiryDate < expiryTo.Value);

        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "remaining" => SortDir(data, b => b.QuantityAvailable.Value, request.SortDir),
            "batch" or "batchnumber" => SortDir(data, b => b.BatchNumber, request.SortDir),
            _ => SortDir(data, b => b.ExpiryDate, request.SortDir)
        };

        var totalCount = await _executor.CountAsync(sorted, cancellationToken);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var rows = await _executor.ToListAsync(
            sorted.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(b => new ExpiryAlertRow(
                    b.Id,
                    b.MedicineVariant!.Medicine!.Name,
                    b.MedicineVariant!.Medicine!.NameAr,
                    b.MedicineVariant!.Form,
                    b.MedicineVariant!.Unit,
                    b.MedicineVariant!.Strength,
                    b.BatchNumber,
                    b.ExpiryDate,
                    b.ExpiryDate.DayNumber - asOf.DayNumber,
                    b.QuantityAvailable.Value)),
            cancellationToken);

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<ExpiryAlertDto>>.Success(items);
    }

    private static (DateOnly? From, DateOnly? To) GetExpiryRange(string? status, DateOnly asOf)
        => status?.ToLowerInvariant() switch
        {
            "expired" => (null, asOf),
            "critical" => (asOf, asOf.AddDays(CriticalWithinDays)),
            "warning" => (asOf.AddDays(CriticalWithinDays), asOf.AddDays(WarningWithinDays)),
            "safe" => (asOf.AddDays(WarningWithinDays), null),
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
