namespace Domain.Exceptions
{
    public sealed class InvalidRefreshTokenException : DomainException
    {
        public InvalidRefreshTokenException()
            : base("The refresh token is invalid, expired or has been revoked.") { }

        public InvalidRefreshTokenException(string message) : base(message) { }
    }
}