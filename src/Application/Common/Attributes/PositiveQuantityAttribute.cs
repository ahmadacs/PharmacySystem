using System.ComponentModel.DataAnnotations;

namespace Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PositiveQuantityAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
        => value is int number && number > 0;

    public override string FormatErrorMessage(string name)
        => $"{name} must be a positive quantity.";
}