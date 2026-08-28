namespace Domain.Exceptions
{
    public sealed class InvalidCredentialsException : DomainException
    {
        public InvalidCredentialsException()
            : base("The email or password is incorrect.") { }

        public InvalidCredentialsException(string message) : base(message) { }
    }
}