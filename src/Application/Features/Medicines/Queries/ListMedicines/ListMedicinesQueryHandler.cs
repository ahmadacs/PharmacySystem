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

        var paged = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => await _repo.PagedAsync(MedicineProjections.ToListRow(asOf), predicate, m => m.CreatedAt, request.ToPagination(), cancellationToken),
            "category" => await _repo.PagedAsync(MedicineProjections.ToListRow(asOf), predicate, m => m.CategoryEnum, request.ToPagination(), cancellationToken),
            "form" => await _repo.PagedAsync(MedicineProjections.ToListRow(asOf), predicate, m => m.Variants.Where(v => v.IsActive).OrderBy(v => v.Form).Select(v => (Domain.Enums.MedicineForm?)v.Form).FirstOrDefault(), request.ToPagination(), cancellationToken),
            _ => await _repo.PagedAsync(MedicineProjections.ToListRow(asOf), predicate, m => m.Name, request.ToPagination(), cancellationToken)
        };

        var items = paged.Select(r => r.ToDto());

        return Result<PagedList<MedicineListItemDto>>.Success(items);
    }
}
