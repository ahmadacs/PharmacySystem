using Application.Resources;
using Domain.Enums;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Common;

/// <summary>
/// Converts a <see cref="PrescriptionStatus"/> enum value into its localized
/// display string. Localizer format arguments must always be display-ready
/// text — never pass a raw enum into <c>_localizer[...]</c>.
/// </summary>
public static class PrescriptionStatusDisplay
{
    public static string ToDisplayName(PrescriptionStatus status, IStringLocalizer<SharedResource> localizer)
        => status switch
        {
            PrescriptionStatus.Pending => localizer["PrescriptionStatus_Pending"],
            PrescriptionStatus.PartiallyDispensed => localizer["PrescriptionStatus_PartiallyDispensed"],
            PrescriptionStatus.FullyDispensed => localizer["PrescriptionStatus_FullyDispensed"],
            PrescriptionStatus.Cancelled => localizer["PrescriptionStatus_Cancelled"],
            PrescriptionStatus.Expired => localizer["PrescriptionStatus_Expired"],
            _ => status.ToString()
        };
}
