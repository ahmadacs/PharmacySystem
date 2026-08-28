namespace Domain.Exceptions
{
    public sealed class RefillNotEligibleException : DomainException
    {
        public RefillNotEligibleException(string message) : base(message) { }
    }
}
