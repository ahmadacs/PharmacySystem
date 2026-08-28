namespace Domain.Exceptions;

public sealed class FileValidationException : DomainException
{
    public FileValidationException(string message) : base(message) { }
}
