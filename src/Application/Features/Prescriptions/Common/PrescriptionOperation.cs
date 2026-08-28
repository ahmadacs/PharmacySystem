namespace Application.Features.Prescriptions.Common;

/// <summary>
/// The kind of access a request needs on a Prescription resource. Drives both the
/// resource-based authorization handler and the ownership pipeline behavior.
/// </summary>
public enum PrescriptionOperation
{
    /// <summary>Read-only access (viewing a prescription).</summary>
    View,

    /// <summary>State-changing access (cancel, refill, ...).</summary>
    Manage
}