namespace Application.Features.Prescriptions.Common;

/// <summary>
/// Marker interface for requests that operate on a single owned Prescription.
/// The <c>PrescriptionOwnershipBehavior</c> loads the entity and enforces the
/// resource-ownership rule before the handler runs, so no handler can forget the
/// check (see RefillPrescriptionCommandHandler for the bug this prevents).
/// </summary>
public interface IOwnedPrescriptionRequest
{
    Guid PrescriptionId { get; }

    PrescriptionOperation Operation { get; }
}