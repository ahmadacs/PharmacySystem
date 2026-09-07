using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed class ListMedicinesQueryHandler : IRequestHandler<ListMedicinesQuery, PagedList<MedicineListItemDto>>
{
    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;

    public ListMedicinesQueryHandler(IMedicineRepository repo, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _executor = executor;
    }

    public async Task<PagedList<MedicineListItemDto>> Handle(ListMedicinesQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<Medicine> baseQuery = _repo.Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            baseQuery = baseQuery.Where(m =>
                m.Name.Contains(search) ||
                (m.NameAr != null && m.NameAr.Contains(search)) ||
                (m.GenericName != null && m.GenericName.Name.Contains(search)) ||
                (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)) ||
                m.CategoryEnum.ToString().Contains(search));
        }

        if (request.CategoryId.HasValue)
            baseQuery = baseQuery.Where(m => (int)m.CategoryEnum == request.CategoryId.Value);

        if (request.Form.HasValue)
            baseQuery = baseQuery.Where(m => m.Variants.Any(v => v.Form == request.Form.Value && !v.IsDeleted));

        if (request.IsActive.HasValue)
            baseQuery = baseQuery.Where(m => m.IsActive == request.IsActive.Value);

        baseQuery = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => SortDir(baseQuery, m => m.CreatedAt, request.SortDir),
            "category" => SortDir(baseQuery, m => m.CategoryEnum, request.SortDir),
            "form" => SortDir(baseQuery, m => m.Variants.Where(v => !v.IsDeleted).OrderBy(v => v.Form).Select(v => v.Form).FirstOrDefault(), request.SortDir),
            _ => SortDir(baseQuery, m => m.Name, request.SortDir)
        };

        var projected = baseQuery
            .Select(m => new MedicineRow(
                m.Id,
                m.Name,
                m.NameAr,
                m.GenericName != null ? m.GenericName.Name : string.Empty,
                m.GenericName != null ? m.GenericName.NameAr : null,
                m.CategoryEnum,
                m.IsControlled,
                m.IsActive,
                m.Variants
                    .Where(v => !v.IsDeleted && v.IsActive)
                    .OrderBy(v => v.Form)
                    .Select(v => new MedicineVariantRow(
                        v.Id,
                        v.Form,
                        v.Unit,
                        v.Strength,
                        v.Batches.Where(b => !b.IsDeleted && b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                        v.ReorderLevel.Value,
                        v.UnitOfMeasure.BaseUnitName,
                        v.UnitOfMeasure.PackageUnitName,
                        v.UnitOfMeasure.UnitsPerPackage,
                        v.UnitOfMeasure.IsDivisible))
                    .ToList(),
                m.Variants.Count(v => !v.IsDeleted && v.IsActive)));

        var totalCount = await _executor.CountAsync(projected, cancellationToken);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var rows = await _executor.ToListAsync(
            projected.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken);

        return rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);
    }

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
