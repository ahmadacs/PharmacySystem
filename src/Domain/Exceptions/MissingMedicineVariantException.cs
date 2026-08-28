namespace Domain.Exceptions
{
    public sealed class MissingMedicineVariantException : DomainException
    {
        public MissingMedicineVariantException(string message) : base(message) { }
    }
}