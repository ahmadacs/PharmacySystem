namespace Domain.Exceptions
{
    public sealed class AccountDisabledException : DomainException
    {
        public AccountDisabledException()
            : base("This account has been disabled. Contact an administrator.") { }
    }
}