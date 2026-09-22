namespace Domain.Common;

/// <summary>
/// Canonical Saudi mobile handling. The canonical stored/searched form is
/// always <c>+9665XXXXXXXX</c>, so <c>05XXXXXXXX</c>, <c>5XXXXXXXX</c>,
/// <c>009665XXXXXXXX</c> and <c>+9665XXXXXXXX</c> all resolve to the same
/// patient instead of creating duplicates. BCL only (Domain rule).
/// Non-Saudi-looking input is returned cleaned but untouched.
/// </summary>
public static class PhoneNumbers
{
    public static string NormalizeSaudiPhone(string? phoneNumber)
    {
        var cleaned = (phoneNumber ?? string.Empty).Trim().Replace(" ", "").Replace("-", "");
        if (cleaned.StartsWith("00", StringComparison.Ordinal) && cleaned.Length > 2)
            cleaned = "+" + cleaned[2..];
        if (cleaned.StartsWith("+966", StringComparison.Ordinal))
            return cleaned;
        if (cleaned.Length == 10 && cleaned.StartsWith("05", StringComparison.Ordinal))
            return "+966" + cleaned[1..];
        if (cleaned.Length == 9 && cleaned.StartsWith("5", StringComparison.Ordinal))
            return "+966" + cleaned;
        return cleaned;
    }
}
