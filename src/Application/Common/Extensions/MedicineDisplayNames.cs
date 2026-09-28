using System.Globalization;
using Domain.Entities.Medicines;

namespace Application.Common.Extensions;

public static class MedicineDisplayNames
{
    public static string Resolve(Medicine? medicine, CultureInfo culture, string unknown)
        => Resolve(medicine?.Name, medicine?.NameAr, culture, unknown);

    public static string Resolve(string? name, string? nameAr, CultureInfo culture, string unknown)
    {
        if (string.IsNullOrWhiteSpace(name))
            return unknown;
        if (culture.TwoLetterISOLanguageName == "ar" && !string.IsNullOrWhiteSpace(nameAr))
            return nameAr;
        return name;
    }
}
