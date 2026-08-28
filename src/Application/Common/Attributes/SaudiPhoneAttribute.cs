using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Application.Common.Attributes;

/// <summary>Validates Saudi phone numbers: +9665XXXXXXXX or 05XXXXXXXX or 5XXXXXXXX (9 digits after 5).</summary>
public sealed class SaudiPhoneAttribute : ValidationAttribute
{
    private static readonly Regex Pattern = new(@"^(?:\+9665\d{8}|05\d{8}|5\d{8})$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success; // [Required] handles null
        if (value is not string s) return new ValidationResult("Phone number must be a string.");
        s = s.Trim().Replace(" ", "").Replace("-", "");
        if (string.IsNullOrEmpty(s)) return ValidationResult.Success;
        if (!Pattern.IsMatch(s))
            return new ValidationResult(ErrorMessage ?? "Phone number must be a valid Saudi number (+9665XXXXXXXX or 05XXXXXXXX).");
        return ValidationResult.Success;
    }
}
