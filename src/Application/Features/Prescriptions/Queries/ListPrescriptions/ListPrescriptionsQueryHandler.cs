using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using Application.Features.Prescriptions.Dtos;
using MediatR;

namespace Application.Features.Prescriptions.Queries;

public sealed class ListPrescriptionsQueryHandler : IRequestHandler<ListPrescriptionsQuery, Result<PagedList<PrescriptionListItemDto>>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;

    public ListPrescriptionsQueryHandler(
        IPrescriptionRepository prescriptions,
        ICurrentUserService currentUser,
        IStaffService staff)
    {
        _prescriptions = prescriptions;
        _currentUser = currentUser;
        _staff = staff;
    }

    public async Task<Result<PagedList<PrescriptionListItemDto>>> Handle(
        ListPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        Guid? restrictedToDoctorId = null;

        if (PrescriptionAccess.CanManageOwn(_currentUser) && !PrescriptionAccess.CanViewAll(_currentUser))
        {
            var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
            if (authResult.IsSuccess)
            {
                var userId = authResult.Value;
                restrictedToDoctorId = await _staff.GetDoctorIdForUserAsync(userId, cancellationToken);
            }
            else
            {
                return Result<PagedList<PrescriptionListItemDto>>.Failure(authResult.Error!, 403);
            }
        }

        var page = await _prescriptions.ListAsync(request, restrictedToDoctorId, cancellationToken);
        return Result<PagedList<PrescriptionListItemDto>>.Success(page);
    }
}