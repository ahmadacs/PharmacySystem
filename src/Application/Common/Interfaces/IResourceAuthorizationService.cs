using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;

namespace Application.Common.Interfaces;

/// <summary>
/// Resource-based authorization enforcing the "own records only" rule. The
/// implementation lives in the Infrastructure layer where it drives ASP.NET Core's
/// IAuthorizationService + IAuthorizationHandler machinery, keeping the Application
/// layer dependent only on Domain. The check is fail-fast: denial throws
/// <see cref="Domain.Exceptions.ForbiddenResourceException"/>.
/// </summary>
public interface IResourceAuthorizationService
{
    /// <summary>
    /// Ensures the current user may perform <paramref name="operation"/> on the
    /// given prescription, otherwise throws
    /// <see cref="Domain.Exceptions.ForbiddenResourceException"/>.
    /// </summary>
    Task EnsureCanAccessPrescriptionAsync(
        Prescription prescription,
        PrescriptionOperation operation,
        CancellationToken cancellationToken);
}