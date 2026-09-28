using Application.Features.Prescriptions.Common;
using Domain.Entities.Prescriptions;

namespace Application.Common.Interfaces;

public interface IResourceAuthorizationService
{

    Task EnsureCanAccessPrescriptionAsync(
        Prescription prescription,
        PrescriptionOperation operation,
        CancellationToken cancellationToken);
}