using System.ComponentModel.DataAnnotations;

namespace Application.Common.Attributes;

/// <summary>
/// Validates that a DateOnly/DateTime value is not in the past (today or later).
/// Used e.g. on a medicine batch expiry date — a rule the built-in attributes
/// cannot express. Also rejects the default value (year 1) when the date is
/// omitted from the request.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotInThePastAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        var today = DateOnly.FromDateTime(DateTime.Today);

        return value switch
        {
            DateOnly date => date >= today,
            DateTime dateTime => DateOnly.FromDateTime(dateTime) >= today,
            string str when DateOnly.TryParse(str, out var parsed) => parsed >= today,
            _ => false
        };
    }

    public override string FormatErrorMessage(string name)
        => $"{name} must not be in the past.";
}