using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class ExpiryAlertListQueryHandler : IRequestHandler<ExpiryAlertListQuery, Result<PagedList<ExpiryAlertDto>>>
{
    private const int CriticalWithinDays = 30;
    private const int WarningWithinDays = 90;

    private readonly IBaseRepository<MedicineBatch> _repo;

    public ExpiryAlertListQueryHandler(IBaseRepository<MedicineBatch> repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<ExpiryAlertDto>>> Handle(ExpiryAlertListQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var (expiryFrom, expiryTo) = GetExpiryRange(request.Status, asOf);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var spec = new Specification<MedicineBatch, ExpiryAlertRow>(b => new ExpiryAlertRow(
                    b.Id,
                    b.MedicineVariant!.Medicine!.Name,
                    b.MedicineVariant!.Medicine!.NameAr,
                    b.MedicineVariant!.Form,
                    b.MedicineVariant!.Unit,
                    b.MedicineVariant!.Strength,
                    b.BatchNumber,
                    b.ExpiryDate,
                    b.ExpiryDate.DayNumber - asOf.DayNumber,
                    b.QuantityAvailable.Value));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            spec.Where(b =>
                b.BatchNumber.Contains(search) ||
                b.MedicineVariant!.Medicine!.Name.Contains(search));
        }

        if (expiryFrom.HasValue)
            spec.Where(b => b.ExpiryDate >= expiryFrom.Value);

        if (expiryTo.HasValue)
            spec.Where(b => b.ExpiryDate < expiryTo.Value);

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "remaining" => SortDir(b => b.QuantityAvailable.Value, request.SortDir),
            "batch" or "batchnumber" => SortDir(b => b.BatchNumber, request.SortDir),
            _ => SortDir(b => b.ExpiryDate, request.SortDir)
        });

        var totalCount = await _repo.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _repo.ListAsync(spec, cancellationToken);

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

    private static Func<IQueryable<MedicineBatch>, IOrderedQueryable<MedicineBatch>> SortDir<TKey>(
        System.Linq.Expressions.Expression<Func<MedicineBatch, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q => q.OrderByDescending(keySelector)
            : q => q.OrderBy(keySelector);
}
