using System.Text.RegularExpressions;

namespace Domain.ValueObjects;

public sealed partial record LicenseNumber
{
    public string Value { get; }

    private LicenseNumber(string value) => Value = value;

    public static LicenseNumber Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("License number is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!LicensePattern().IsMatch(normalized))
            throw new ArgumentException("License number must be 3-20 alphanumeric characters (placeholder rule — confirm real format).", nameof(value));

        return new LicenseNumber(normalized);
    }

    [GeneratedRegex("^[A-Z0-9]{3,20}$")]
    private static partial Regex LicensePattern();

    public override string ToString() => Value;
}