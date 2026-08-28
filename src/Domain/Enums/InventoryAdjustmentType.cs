namespace Domain.Enums
{
    /// <summary>
    /// The kind of stock movement recorded on an inventory adjustment.
    /// Values are stored as strings in the database (see the EF configuration).
    /// </summary>
    public enum InventoryAdjustmentType
    {
        Increase = 1,
        Decrease = 2,
        Correction = 3,
        Damaged = 4,
        Expired = 5,
        Returned = 6,
        Sold = 7,
        TransferOut = 8,
        TransferIn = 9
    }
}