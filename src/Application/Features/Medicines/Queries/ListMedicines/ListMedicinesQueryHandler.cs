using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed class ListMedicinesQueryHandler : IRequestHandler<ListMedicinesQuery, Result<PagedList<MedicineListItemDto>>>
{
    private readonly IBaseRepository<Medicine> _repo;

    public ListMedicinesQueryHandler(IBaseRepository<Medicine> repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<MedicineListItemDto>>> Handle(ListMedicinesQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);

        var spec = new Specification<Medicine, MedicineRow>(m => new MedicineRow(
                m.Id,
                m.Name,
                m.NameAr,
                m.GenericName != null ? m.GenericName.Name : string.Empty,
                m.GenericName != null ? m.GenericName.NameAr : null,
                m.CategoryEnum,
                m.IsControlled,
                m.IsActive,
                m.Variants
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Form)
                    .Select(v => new MedicineVariantRow(
                        v.Id,
                        v.Form,
                        v.Unit,
                        v.Strength,
                        v.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0,
                        v.ReorderLevel.Value,
                        v.UnitOfMeasure.BaseUnitName,
                        v.UnitOfMeasure.PackageUnitName,
                        v.UnitOfMeasure.UnitsPerPackage,
                        v.UnitOfMeasure.IsDivisible))
                    .ToList(),
                m.Variants.Count(v => v.IsActive)));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            spec.Where(m =>
                m.Name.Contains(search) ||
                (m.NameAr != null && m.NameAr.Contains(search)) ||
                (m.GenericName != null && m.GenericName.Name.Contains(search)) ||
                (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)) ||
                m.CategoryEnum.ToString().Contains(search));
        }

        if (request.CategoryId.HasValue)
            spec.Where(m => (int)m.CategoryEnum == request.CategoryId.Value);

        if (request.Form.HasValue)
            spec.Where(m => m.Variants.Any(v => v.Form == request.Form.Value));

        if (request.IsActive.HasValue)
            spec.Where(m => m.IsActive == request.IsActive.Value);

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => SortDir(m => m.CreatedAt, request.SortDir),
            "category" => SortDir(m => m.CategoryEnum, request.SortDir),
            "form" => SortDir(m => m.Variants.OrderBy(v => v.Form).Select(v => v.Form).FirstOrDefault(), request.SortDir),
            _ => SortDir(m => m.Name, request.SortDir)
        });

        // COUNT ignores ordering/paging/selector: same single COUNT query as before.
        var totalCount = await _repo.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _repo.ListAsync(spec, cancellationToken);

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<MedicineListItemDto>>.Success(items);
    }

    private static Func<IQueryable<Medicine>, IOrderedQueryable<Medicine>> SortDir<TKey>(
        System.Linq.Expressions.Expression<Func<Medicine, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q => q.OrderByDescending(keySelector)
            : q => q.OrderBy(keySelector);
}
