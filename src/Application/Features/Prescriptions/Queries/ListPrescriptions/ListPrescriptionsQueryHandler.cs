using System.Linq.Expressions;
using Application.Common.Security;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
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

        if (_currentUser.Permissions.Contains(Permissions.Prescriptions.ManageOwn)
            && !_currentUser.Permissions.Contains(Permissions.Prescriptions.View))
        {
            var authFailure = AuthGuard.RequireUserId<PagedList<PrescriptionListItemDto>>(_currentUser, _localizer, out var userId);
            if (authFailure is not null)
                return authFailure;

            restrictedToDoctorId = await _staff.GetDoctorIdForUserAsync(userId, cancellationToken);
        }

        var selector = (Expression<Func<Prescription, PrescriptionListRow>>)(p => new PrescriptionListRow(
                    p.Id,
                    p.ShortCode,
                    p.DoctorId,
                    p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
                    p.Patient != null ? p.Patient.DateOfBirth : default,
                    p.Patient != null ? p.Patient.PhoneNumber : null,
                    p.IssuedDate,
                    p.Status,
                    p.Items.Count(),
                    p.CreatedAt));

        var doctorId = restrictedToDoctorId;
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var status = request.Status;
        var fromDate = request.FromDate;
        var toDate = request.ToDate;
        Expression<Func<Prescription, bool>> predicate =
            p => (!doctorId.HasValue || p.DoctorId == doctorId.Value)
                && (search == null || (p.Patient != null &&
                    (p.Patient.FirstName.Contains(search) || p.Patient.LastName.Contains(search))))
                && (!status.HasValue || p.Status == status.Value)
                && (!fromDate.HasValue || p.IssuedDate >= fromDate.Value)
                && (!toDate.HasValue || p.IssuedDate <= toDate.Value);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();
        var desc = request.SortDir.IsDescending();

        var totalCount = await _prescriptions.CountAsync(predicate, cancellationToken);
        List<PrescriptionListRow> rows = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => await _prescriptions.PagedAsync(selector, predicate, p => p.CreatedAt, desc, page, pageSize, cancellationToken),
            "patientname" => await _prescriptions.PagedAsync(selector, predicate, p => p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty, desc, page, pageSize, cancellationToken),
            "status" => await _prescriptions.PagedAsync(selector, predicate, p => p.Status, desc, page, pageSize, cancellationToken),
            _ => await _prescriptions.PagedAsync(selector, predicate, p => p.IssuedDate, desc, page, pageSize, cancellationToken)
        };

        var doctorNamesById = await _staff.GetDoctorNamesAsync(
            rows.Select(p => p.DoctorId).Distinct().Where(id => id != Guid.Empty).ToList(),
            cancellationToken);

        var items = rows
            .Select(p => p.ToDto(doctorNamesById.GetValueOrDefault(p.DoctorId, string.Empty)))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<PrescriptionListItemDto>>.Success(items);
    }
}
