namespace Application.Features.Prescriptions.Common;

public interface IOwnedPrescriptionRequest
{
    Guid PrescriptionId { get; }

    PrescriptionOperation Operation { get; }
}