namespace Domain.Exceptions
{
    public class RefillNotEligibleException : DomainException
    {
        public RefillNotEligibleException(string message) : base(message) { }
    }
}
