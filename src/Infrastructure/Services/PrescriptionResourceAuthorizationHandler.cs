using Application.Common.Interfaces;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Services;

/// <summary>
/// Resource-based requirement for a specific operation on a Prescription. The
/// operation lets one handler enforce both read and manage ownership rules.
/// </summary>
public sealed class OwnPrescriptionRequirement : IAuthorizationRequirement
{
    public PrescriptionOperation Operation { get; }

    public OwnPrescriptionRequirement(PrescriptionOperation operation)
    {
        Operation = operation;
    }
}

/// <summary>
/// Enforces the "own records only" rule on a Prescription resource:
/// <list type="bullet">
/// <item>Holders of <see cref="Permissions.Prescriptions.ManageAll"/> (Admin) may
/// access any prescription for any operation — the only elevated bypass.</item>
/// <item>Holders of <see cref="Permissions.Prescriptions.View"/> may view any
/// prescription (pharmacists, admins) but NOT manage others' prescriptions.</item>
/// <item>Holders of <see cref="Permissions.Prescriptions.ManageOwn"/> may view and
/// manage only prescriptions they issued (the doctor who owns the record).</item>
/// </list>
/// </summary>
public class PrescriptionResourceAuthorizationHandler
    : AuthorizationHandler<OwnPrescriptionRequirement, Prescription>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IStaffService _staff;

    public PrescriptionResourceAuthorizationHandler(ICurrentUserService currentUser, IStaffService staff)
    {
        _currentUser = currentUser;
        _staff = staff;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnPrescriptionRequirement requirement,
        Prescription resource)
    {
        var permissions = _currentUser.Permissions;

        if (permissions.Contains(Permissions.Prescriptions.ManageAll))
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement.Operation == PrescriptionOperation.View
            && permissions.Contains(Permissions.Prescriptions.View))
        {
            context.Succeed(requirement);
            return;
        }

        if (permissions.Contains(Permissions.Prescriptions.ManageOwn)
            && _currentUser.UserId is Guid userId)
        {
            var doctorId = await _staff.GetDoctorIdForUserAsync(userId, CancellationToken.None);
            if (doctorId == resource.DoctorId)
            {
                context.Succeed(requirement);
                return;
            }
        }

        context.Fail(new AuthorizationFailureReason(
            this,
            "You can only view or manage prescriptions you issued."));
    }
}