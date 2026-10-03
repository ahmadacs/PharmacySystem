using Application.Common.Interfaces;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;

namespace Application.Common.Behaviours;

public sealed class PrescriptionOwnershipBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRepository<Prescription> _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly ICurrentUserService _currentUser;

    public PrescriptionOwnershipBehavior(
        IRepository<Prescription> prescriptions,
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
            var prescription = await _prescriptions.GetByIdAsync(owned.PrescriptionId, cancellationToken: cancellationToken)
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

                throw new EntityNotFoundException(typeof(Prescription), owned.PrescriptionId);
            }
        }

        return await next();
    }

    private bool CanSeeExistence()
        => _currentUser.Permissions.Contains(Permissions.Prescriptions.View)
           || _currentUser.Permissions.Contains(Permissions.Prescriptions.ManageAll);
}