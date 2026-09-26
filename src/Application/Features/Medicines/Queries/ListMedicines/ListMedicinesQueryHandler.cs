using System.Linq.Expressions;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
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


        // Lean selector: scientific GenericName + variant type/stock are scalar
        // columns and server-side SUMs (no entity loads). Variant display
        // names are built in MedicineMapping (not SQL).
        var selector = (Expression<Func<Medicine, MedicineRow>>)(m => new MedicineRow(
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
                m.CreatedAt,
                m.Variants.Where(v => v.IsActive).OrderBy(v => v.Form).Select(v => (Domain.Enums.MedicineForm?)v.Form).FirstOrDefault()));

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var categoryId = request.CategoryId;
        var form = request.Form;
        var isActive = request.IsActive;
        Expression<Func<Medicine, bool>> predicate =
            m => (search == null
                    || m.Name.Contains(search)
                    || (m.NameAr != null && m.NameAr.Contains(search))
                    || (m.GenericName != null && m.GenericName.Name.Contains(search))
                    || (m.GenericName != null && m.GenericName.NameAr != null && m.GenericName.NameAr.Contains(search)))
                && (!categoryId.HasValue || (int)m.CategoryEnum == categoryId.Value)
                && (!form.HasValue || m.Variants.Any(v => v.Form == form.Value))
                && (!isActive.HasValue || m.IsActive == isActive.Value);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(100);
        var desc = request.SortDir.IsDescending();

        var totalCount = await _repo.CountAsync(predicate, cancellationToken);
        List<MedicineRow> rows = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => await _repo.PagedAsync(selector, predicate, m => m.CreatedAt, desc, page, pageSize, cancellationToken),
            "category" => await _repo.PagedAsync(selector, predicate, m => m.CategoryEnum, desc, page, pageSize, cancellationToken),
            "form" => await _repo.PagedAsync(selector, predicate, m => m.Variants.Where(v => v.IsActive).OrderBy(v => v.Form).Select(v => (Domain.Enums.MedicineForm?)v.Form).FirstOrDefault(), desc, page, pageSize, cancellationToken),
            _ => await _repo.PagedAsync(selector, predicate, m => m.Name, desc, page, pageSize, cancellationToken)
        };

        var items = rows
            .Select(r => r.ToDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<MedicineListItemDto>>.Success(items);
    }
}
