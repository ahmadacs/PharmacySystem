using Domain.Enums;

namespace Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public sealed class ConflictingOperationException : DomainException
{
    public ConflictingOperationException(string message) : base(message) { }
}

public sealed class EntityNotFoundException : DomainException
{
    public Type EntityType { get; }
    public Guid EntityId { get; }

    public EntityNotFoundException(Type entityType, Guid entityId)
        : base($"Resource '{entityType.Name}' with id '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

public sealed class ExpiredBatchException : DomainException
{
    public Guid MedicineBatchId { get; }
    public DateOnly ExpiryDate { get; }

    public string? BatchNumber { get; }

    public ExpiredBatchException(Guid medicineBatchId, DateOnly expiryDate, string? batchNumber = null)
        : base($"Medicine batch '{medicineBatchId}' expired on {expiryDate:yyyy-MM-dd} and cannot be dispensed.")
    {
        MedicineBatchId = medicineBatchId;
        ExpiryDate = expiryDate;
        BatchNumber = batchNumber;
    }
}

public sealed class FileValidationException : DomainException
{
    public FileValidationException(string message) : base(message) { }
}

public sealed class InvalidBatchDatesException : DomainException
{
    public DateOnly ManufactureDate { get; }
    public DateOnly ExpiryDate { get; }

    public InvalidBatchDatesException(DateOnly manufactureDate, DateOnly expiryDate)
        : base($"Expiry date '{expiryDate:yyyy-MM-dd}' must be after the manufacture date '{manufactureDate:yyyy-MM-dd}'.")
    {
        ManufactureDate = manufactureDate;
        ExpiryDate = expiryDate;
    }
}

public sealed class ForbiddenResourceException : DomainException
{
    public ForbiddenResourceException(string message = "You are not allowed to access this resource.")
        : base(message) { }
}

public sealed class InsufficientStockException : DomainException
{
    public Guid MedicineBatchId { get; }
    public int Requested { get; }
    public int Available { get; }

    public string? MedicineName { get; }
    public string? MedicineNameAr { get; }

    public InsufficientStockException(Guid medicineBatchId, int requested, int available, string? medicineName = null, string? medicineNameAr = null)
        : base($"Insufficient stock (id '{medicineBatchId}'). Requested {requested}, available {available}.")
    {
        MedicineBatchId = medicineBatchId;
        Requested = requested;
        Available = available;
        MedicineName = medicineName;
        MedicineNameAr = medicineNameAr;
    }
}

public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("The email or password is incorrect.") { }

    public InvalidCredentialsException(string message) : base(message) { }
}

public sealed class InvalidPrescriptionStatusException : DomainException
{
    public Guid PrescriptionId { get; }
    public Guid? PrescriptionItemId { get; }
    public PrescriptionStatus Status { get; }
    public PrescriptionStatusReason Reason { get; }

    public InvalidPrescriptionStatusException(
        PrescriptionStatusReason reason,
        PrescriptionStatus status,
        Guid prescriptionId,
        Guid? prescriptionItemId = null)
        : base($"Prescription '{prescriptionId}' status violation '{reason}' while in '{status}' status.")
    {
        Reason = reason;
        Status = status;
        PrescriptionId = prescriptionId;
        PrescriptionItemId = prescriptionItemId;
    }
}

public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("The refresh token is invalid, expired or has been revoked.") { }

    public InvalidRefreshTokenException(string message) : base(message) { }
}

public sealed class MissingMedicineVariantException : DomainException
{
    public Guid MedicineVariantId { get; }
    public Guid PrescriptionItemId { get; }

    public MissingMedicineVariantException(Guid medicineVariantId, Guid prescriptionItemId)
        : base($"MedicineVariant '{medicineVariantId}' for prescription item '{prescriptionItemId}' was not provided to the dispensing service.")
    {
        MedicineVariantId = medicineVariantId;
        PrescriptionItemId = prescriptionItemId;
    }
}

public class RefillNotEligibleException : DomainException
{
    public Guid PrescriptionItemId { get; }
    public RefillEligibilityReason Reason { get; }
    public int RefillsUsed { get; }
    public int RefillsAllowed { get; }

    public RefillNotEligibleException(
        Guid prescriptionItemId,
        RefillEligibilityReason reason,
        int refillsUsed = 0,
        int refillsAllowed = 0)
        : base($"Prescription item '{prescriptionItemId}' is not eligible for refill (reason '{reason}').")
    {
        PrescriptionItemId = prescriptionItemId;
        Reason = reason;
        RefillsUsed = refillsUsed;
        RefillsAllowed = refillsAllowed;
    }
}

public sealed class RefillIntervalNotSatisfiedException : RefillNotEligibleException
{
    public DateOnly NextEligibleDate { get; }

    public string? MedicineName { get; }
    public string? MedicineNameAr { get; }

    public RefillIntervalNotSatisfiedException(
        Guid prescriptionItemId,
        DateOnly nextEligibleDate,
        string? medicineName = null,
        string? medicineNameAr = null)
        : base(prescriptionItemId, RefillEligibilityReason.IntervalNotSatisfied)
    {
        NextEligibleDate = nextEligibleDate;
        MedicineName = medicineName;
        MedicineNameAr = medicineNameAr;
    }
}
