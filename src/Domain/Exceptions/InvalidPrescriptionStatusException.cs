namespace Domain.Exceptions
{
    public sealed class InvalidPrescriptionStatusException : DomainException
    {
        public InvalidPrescriptionStatusException(string message) : base(message) { }
    }
}
