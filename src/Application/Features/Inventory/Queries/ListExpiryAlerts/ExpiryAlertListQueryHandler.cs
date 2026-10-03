using System.Linq.Expressions;
using Application.Common.Extensions;
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

    private readonly IRepository<MedicineBatch> _repo;

    public ExpiryAlertListQueryHandler(IRepository<MedicineBatch> repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedList<ExpiryAlertDto>>> Handle(ExpiryAlertListQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var (expiryFrom, expiryTo) = GetExpiryRange(request.Status, asOf);

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        System.Linq.Expressions.Expression<Func<MedicineBatch, bool>> predicate =
            b => (search == null || b.BatchNumber.Contains(search) || b.MedicineVariant!.Medicine!.Name.Contains(search))
                && (!expiryFrom.HasValue || b.ExpiryDate >= expiryFrom.Value)
                && (!expiryTo.HasValue || b.ExpiryDate < expiryTo.Value);

        var paged = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" or "remaining" => await _repo.PagedAsync(ExpiryAlertProjection, predicate, b => b.QuantityAvailable.Value, request.ToPagination(), cancellationToken),
            "batch" or "batchnumber" => await _repo.PagedAsync(ExpiryAlertProjection, predicate, b => b.BatchNumber, request.ToPagination(), cancellationToken),
            _ => await _repo.PagedAsync(ExpiryAlertProjection, predicate, b => b.ExpiryDate, request.ToPagination(), cancellationToken)
        };

        var items = paged.Select(r => r.ToDto(asOf));

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

    private static readonly Expression<Func<MedicineBatch, ExpiryAlertRow>> ExpiryAlertProjection = b => new ExpiryAlertRow(
        b.Id,
        b.MedicineVariant!.Medicine!.Name,
        b.MedicineVariant!.Medicine!.NameAr,
        b.MedicineVariant!.Form,
        b.MedicineVariant!.Unit,
        b.MedicineVariant!.Strength,
        b.BatchNumber,
        b.ExpiryDate,
        b.QuantityAvailable.Value);
}
