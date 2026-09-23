namespace Domain.Common;

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
