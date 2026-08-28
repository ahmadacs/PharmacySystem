namespace Domain.Exceptions
{
    public sealed class InsufficientStockException : DomainException
    {
        public Guid MedicineBatchId { get; }
        public int Requested { get; }
        public int Available { get; }

        public InsufficientStockException(Guid medicineBatchId, int requested, int available)
            : base($"Insufficient stock in batch '{medicineBatchId}'. Requested {requested}, available {available}.")
        {
            MedicineBatchId = medicineBatchId;
            Requested = requested;
            Available = available;
        }
    }
}
