using Application.Common.Interfaces;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;

namespace Application.Common.Behaviours;

/// <summary>
/// Enforces resource-based authorization for every request marked
/// <see cref="IOwnedPrescriptionRequest"/>. The entity is loaded once here and
/// checked against the current user's access rights BEFORE the handler runs, so
/// ownership checks can never be forgotten in a new handler. Requests that do not
/// implement the marker pass straight through.
/// </summary>
public sealed class PrescriptionOwnershipBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly ICurrentUserService _currentUser;

    public PrescriptionOwnershipBehavior(
        IPrescriptionRepository prescriptions,
        IResourceAuthorizationService resourceAuth,
        ICurrentUserService currentUser)
    {
        _prescriptions = prescriptions;
        _resourceAuth = resourceAuth;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IOwnedPrescriptionRequest owned)
        {
            var prescription = await _prescriptions.GetByIdAsync(owned.PrescriptionId, cancellationToken)
                ?? throw new EntityNotFoundException(typeof(Prescription), owned.PrescriptionId);

            try
            {
                await _resourceAuth.EnsureCanAccessPrescriptionAsync(
                    prescription,
                    owned.Operation,
                    cancellationToken);
            }
            catch (ForbiddenResourceException) when (!CanSeeExistence())
            {
                // A caller that cannot enumerate prescriptions (no View) must not be
                // able to tell "this id does not exist" (404) apart from "this id
                // exists but belongs to someone else" (403). Healthcare data is
                // sensitive, so both cases return 404. See README §15.
                throw new EntityNotFoundException(typeof(Prescription), owned.PrescriptionId);
            }
        }

        return await next();
    }

    /// <summary>
    /// Users who can enumerate prescriptions (<c>View</c>) or bypass ownership
    /// entirely (<c>ManageAll</c>) legitimately know a prescription exists, so for
    /// them a denial stays a genuine 403 (e.g. a pharmacist may view but not manage).
    /// Everyone else receives the same 404 as a non-existent id.
    /// </summary>
    private bool CanSeeExistence()
        => _currentUser.Permissions.Contains(Permissions.Prescriptions.View)
           || _currentUser.Permissions.Contains(Permissions.Prescriptions.ManageAll);
}