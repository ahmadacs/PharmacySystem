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
    private readonly IRepository<Prescription> _prescriptions;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListPrescriptionsQueryHandler(
        IRepository<Prescription> prescriptions,
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

        var paged = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => await _prescriptions.PagedAsync(PrescriptionProjections.ToListRow, predicate, p => p.CreatedAt, request.ToPagination(), cancellationToken),
            "patientname" => await _prescriptions.PagedAsync(PrescriptionProjections.ToListRow, predicate, p => p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty, request.ToPagination(), cancellationToken),
            "status" => await _prescriptions.PagedAsync(PrescriptionProjections.ToListRow, predicate, p => p.Status, request.ToPagination(), cancellationToken),
            _ => await _prescriptions.PagedAsync(PrescriptionProjections.ToListRow, predicate, p => p.IssuedDate, request.ToPagination(), cancellationToken)
        };

        var rows = paged.Items;

        var doctorNamesById = await _staff.GetDoctorNamesAsync(
            rows.Select(p => p.DoctorId).Distinct().Where(id => id != Guid.Empty).ToList(),
            cancellationToken);

        var items = paged.Select(p => p.ToDto(doctorNamesById.GetValueOrDefault(p.DoctorId, string.Empty)));

        return Result<PagedList<PrescriptionListItemDto>>.Success(items);
    }
}
