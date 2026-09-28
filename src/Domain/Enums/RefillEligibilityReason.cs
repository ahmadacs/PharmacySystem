namespace Domain.Enums;

public enum RefillEligibilityReason
{
    NotRefillable = 0,
    NotFullyDispensed = 1,
    Exhausted = 2,
    IntervalNotSatisfied = 3
}
