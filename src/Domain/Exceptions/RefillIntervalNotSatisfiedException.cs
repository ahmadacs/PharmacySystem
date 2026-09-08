namespace Domain.Exceptions
{
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
}
