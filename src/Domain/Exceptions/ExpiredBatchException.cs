namespace Domain.Exceptions
{
    public sealed class ExpiredBatchException : DomainException
    {
        public Guid MedicineBatchId { get; }
        public DateOnly ExpiryDate { get; }

        public ExpiredBatchException(Guid medicineBatchId, DateOnly expiryDate)
            : base($"Medicine batch '{medicineBatchId}' expired on {expiryDate:yyyy-MM-dd} and cannot be dispensed.")
        {
            MedicineBatchId = medicineBatchId;
            ExpiryDate = expiryDate;
        }
    }
}
