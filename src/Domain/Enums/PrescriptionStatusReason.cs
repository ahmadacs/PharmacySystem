namespace Domain.Enums;

public enum PrescriptionStatusReason
{
    AddItemsProhibited = 0,
    AlreadyCancelled = 1,
    CancelAfterDispensed = 2,
    NotDispensable = 3,
    Empty = 4,
    ItemNotInPrescription = 5,
    RefillProhibited = 6
}
