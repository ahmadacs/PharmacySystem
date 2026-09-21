using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Entities.Prescriptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Queries;

public sealed class ListPrescriptionsQueryHandler : IRequestHandler<ListPrescriptionsQuery, Result<PagedList<PrescriptionListItemDto>>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListPrescriptionsQueryHandler(
        IPrescriptionRepository prescriptions,
        ICurrentUserService currentUser,
        IStaffService staff,
        IAsyncQueryExecutor executor,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _currentUser = currentUser;
        _staff = staff;
        _executor = executor;
        _localizer = localizer;
    }

    public async Task<Result<PagedList<PrescriptionListItemDto>>> Handle(
        ListPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        Guid? restrictedToDoctorId = null;

        if (PrescriptionAccess.CanManageOwn(_currentUser) && !PrescriptionAccess.CanViewAll(_currentUser))
        {
            var authFailure = AuthGuard.RequireUserId<PagedList<PrescriptionListItemDto>>(_currentUser, _localizer, out var userId);
            if (authFailure is not null)
                return authFailure;

            restrictedToDoctorId = await _staff.GetDoctorIdForUserAsync(userId, cancellationToken);
        }

        IQueryable<Prescription> data = _prescriptions.Query();

        if (restrictedToDoctorId.HasValue)
            data = data.Where(p => p.DoctorId == restrictedToDoctorId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            data = data.Where(p => p.Patient != null &&
                (p.Patient.FirstName.Contains(search) || p.Patient.LastName.Contains(search)));
        }

        if (request.Status.HasValue)
            data = data.Where(p => p.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            data = data.Where(p => p.IssuedDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            data = data.Where(p => p.IssuedDate <= request.ToDate.Value);

        var totalCount = await _executor.CountAsync(data, cancellationToken);

        data = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => SortDir(data, p => p.CreatedAt, request.SortDir),
            "patientname" => SortDir(data, p => p.Patient != null ? (p.Patient.LastName + " " + p.Patient.FirstName) : string.Empty, request.SortDir),
            "status" => SortDir(data, p => p.Status, request.SortDir),
            _ => SortDir(data, p => p.IssuedDate, request.SortDir)
        };

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();

        var rows = await _executor.ToListAsync(
            data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PrescriptionListRow(
                    p.Id,
                    p.DoctorId,
                    p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
                    p.Patient != null ? p.Patient.DateOfBirth : default,
                    p.Patient != null ? p.Patient.Age : 0,
                    p.Patient != null ? p.Patient.PhoneNumber : null,
                    p.IssuedDate,
                    p.Status.ToString(),
                    p.Items.Count())),
            cancellationToken);

        var doctorIds = rows.Select(p => p.DoctorId).Distinct().Where(id => id != Guid.Empty).ToList();
        var doctorNamesById = await _staff.GetDoctorNamesAsync(doctorIds, cancellationToken);

        var items = rows
            .Select(p => p.ToDto(doctorNamesById.GetValueOrDefault(p.DoctorId, string.Empty)))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<PrescriptionListItemDto>>.Success(items);
    }

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
