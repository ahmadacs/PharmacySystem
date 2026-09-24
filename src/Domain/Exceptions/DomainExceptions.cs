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

    public ExpiredBatchException(Guid medicineBatchId, DateOnly expiryDate)
        : base($"Medicine batch '{medicineBatchId}' expired on {expiryDate:yyyy-MM-dd} and cannot be dispensed.")
    {
        MedicineBatchId = medicineBatchId;
        ExpiryDate = expiryDate;
    }
}

public sealed class FileValidationException : DomainException
{
    public FileValidationException(string message) : base(message) { }
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

    public InsufficientStockException(Guid medicineBatchId, int requested, int available)
        : base($"Insufficient stock in batch '{medicineBatchId}'. Requested {requested}, available {available}.")
    {
        MedicineBatchId = medicineBatchId;
        Requested = requested;
        Available = available;
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
    public InvalidPrescriptionStatusException(string message) : base(message) { }
}

public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("The refresh token is invalid, expired or has been revoked.") { }

    public InvalidRefreshTokenException(string message) : base(message) { }
}

public sealed class MissingMedicineVariantException : DomainException
{
    public MissingMedicineVariantException(string message) : base(message) { }
}

public class RefillNotEligibleException : DomainException
{
    public RefillNotEligibleException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an item is dispensed before its refill interval has elapsed
/// since the previous dispense. Derives from <see cref="RefillNotEligibleException"/>
/// so every existing 409 mapping (handlers + GlobalExceptionHandler) applies unchanged.
/// </summary>
public sealed class RefillIntervalNotSatisfiedException : RefillNotEligibleException
{
    public DateOnly NextEligibleDate { get; }

    public RefillIntervalNotSatisfiedException(DateOnly nextEligibleDate, string message) : base(message)
    {
        NextEligibleDate = nextEligibleDate;
    }
}
