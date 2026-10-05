namespace Domain.Enums;

public enum NotificationType
{
    LowStock = 1,
    NearExpiry = 2,
    PrescriptionCreated = 3,
    PrescriptionDispensed = 4,
    PrescriptionCancelled = 5,
    PrescriptionRefilled = 6
}