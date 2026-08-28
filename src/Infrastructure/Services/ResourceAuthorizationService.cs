using Application.Common.Interfaces;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

/// <summary>
/// Bridges the Application layer's resource-authorization need to ASP.NET Core's
/// IAuthorizationService and the PrescriptionResourceAuthorizationHandler. Denial
/// fails fast by throwing ForbiddenResourceException (403) — callers can never
/// ignore the outcome.
/// </summary>
public sealed class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly IAuthorizationService _authorization;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResourceAuthorizationService(
        IAuthorizationService authorization,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorization = authorization;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task EnsureCanAccessPrescriptionAsync(
        Prescription prescription,
        PrescriptionOperation operation,
        CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
            throw new ForbiddenResourceException();

        var result = await _authorization.AuthorizeAsync(
            user,
            prescription,
            new OwnPrescriptionRequirement(operation));

        if (!result.Succeeded)
        {
            var message = result.Failure?.FailureReasons.FirstOrDefault()?.Message
                ?? "You are not authorized to access this prescription.";
            throw new ForbiddenResourceException(message);
        }
    }
}