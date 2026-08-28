using System.ComponentModel.DataAnnotations;

namespace Application.Common.Attributes;

/// <summary>
/// Validates that a DateOnly/DateTime value is not in the future (today or
/// earlier). Used e.g. on a patient's date of birth or a prescription issued
/// date, a rule the built-in attributes cannot express.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotInTheFutureAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        var today = DateOnly.FromDateTime(DateTime.Today);

        return value switch
        {
            DateOnly date => date <= today,
            DateTime dateTime => DateOnly.FromDateTime(dateTime) <= today,
            string str when DateOnly.TryParse(str, out var parsed) => parsed <= today,
            _ => false
        };
    }

    public override string FormatErrorMessage(string name)
        => $"{name} must not be in the future.";
}