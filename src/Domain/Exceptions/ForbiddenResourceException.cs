namespace Domain.Exceptions
{
    public sealed class ForbiddenResourceException : DomainException
    {
        public ForbiddenResourceException(string message = "You are not allowed to access this resource.")
            : base(message) { }
    }
}