namespace Domain.Exceptions
{
    public sealed class ConflictingOperationException : DomainException
    {
        public ConflictingOperationException(string message) : base(message) { }
    }
}