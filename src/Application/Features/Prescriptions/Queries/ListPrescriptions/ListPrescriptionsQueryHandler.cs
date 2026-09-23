using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Common.Specifications;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Entities.Prescriptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Queries;

public sealed class ListPrescriptionsQueryHandler : IRequestHandler<ListPrescriptionsQuery, Result<PagedList<PrescriptionListItemDto>>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListPrescriptionsQueryHandler(
        IBaseRepository<Prescription> prescriptions,
        ICurrentUserService currentUser,
        IStaffService staff,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _currentUser = currentUser;
        _staff = staff;
        _localizer = localizer;
    }

    public async Task<Result<PagedList<PrescriptionListItemDto>>> Handle(
        ListPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        Guid? restrictedToDoctorId = null;

        // Doctors without the View permission see only their own prescriptions.
        if (_currentUser.Permissions.Contains(Permissions.Prescriptions.ManageOwn)
            && !_currentUser.Permissions.Contains(Permissions.Prescriptions.View))
        {
            var authFailure = AuthGuard.RequireUserId<PagedList<PrescriptionListItemDto>>(_currentUser, _localizer, out var userId);
            if (authFailure is not null)
                return authFailure;

            restrictedToDoctorId = await _staff.GetDoctorIdForUserAsync(userId, cancellationToken);
        }

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();

        var spec = new Specification<Prescription, PrescriptionListRow>(p => new PrescriptionListRow(
                    p.Id,
                    p.DoctorId,
                    p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
                    p.Patient != null ? p.Patient.DateOfBirth : default,
                    p.Patient != null ? p.Patient.Age : 0,
                    p.Patient != null ? p.Patient.PhoneNumber : null,
                    p.IssuedDate,
                    p.Status.ToString(),
                    p.Items.Count()));

        if (restrictedToDoctorId.HasValue)
            spec.Where(p => p.DoctorId == restrictedToDoctorId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            spec.Where(p => p.Patient != null &&
                (p.Patient.FirstName.Contains(search) || p.Patient.LastName.Contains(search)));
        }

        if (request.Status.HasValue)
            spec.Where(p => p.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            spec.Where(p => p.IssuedDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            spec.Where(p => p.IssuedDate <= request.ToDate.Value);

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => SortDir(p => p.CreatedAt, request.SortDir),
            "patientname" => SortDir(p => p.Patient != null ? (p.Patient.LastName + " " + p.Patient.FirstName) : string.Empty, request.SortDir),
            "status" => SortDir(p => p.Status, request.SortDir),
            _ => SortDir(p => p.IssuedDate, request.SortDir)
        });

        var totalCount = await _prescriptions.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _prescriptions.ListAsync(spec, cancellationToken);

        var doctorIds = rows.Select(p => p.DoctorId).Distinct().Where(id => id != Guid.Empty).ToList();
        var doctorNamesById = await _staff.GetDoctorNamesAsync(doctorIds, cancellationToken);

        var items = rows
            .Select(p => p.ToDto(doctorNamesById.GetValueOrDefault(p.DoctorId, string.Empty)))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<PrescriptionListItemDto>>.Success(items);
    }

    private static Func<IQueryable<Prescription>, IOrderedQueryable<Prescription>> SortDir<TKey>(
        System.Linq.Expressions.Expression<Func<Prescription, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q => q.OrderByDescending(keySelector)
            : q => q.OrderBy(keySelector);
}
