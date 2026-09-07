using Application.Features.Prescriptions.Dtos;

namespace Application.Features.Dashboard.Dtos;

public static class DashboardMapping
{
    /// <summary>Maps a dashboard "latest" row (doctor name resolved separately).</summary>
    public static PrescriptionListItemDto ToListItemDto(this DashboardPrescriptionRow row, string doctorName)
        => new(
            row.Id,
            row.DoctorId,
            doctorName,
            row.PatientName,
            row.PatientDateOfBirth,
            CalculateAge(row.PatientDateOfBirth),
            row.PatientPhoneNumber,
            row.IssuedDate,
            row.Status.ToString(),
            row.IsRefillable,
            row.ItemCount);

    public static DashboardSummaryDto ToDto(
        this DashboardSummarySnapshot snapshot,
        IReadOnlyList<PrescriptionListItemDto> latestPending,
        IReadOnlyList<PrescriptionListItemDto> latestFragmented,
        DateTime generatedAt)
        => new(
            snapshot.DispensedToday,
            snapshot.Pending,
            snapshot.CreatedToday,
            snapshot.LowStock,
            snapshot.ExpiringSoon,
            snapshot.Fragmented,
            generatedAt,
            latestPending,
            latestFragmented);

    /// <summary>Same rule as <c>Patient.Age</c>: full years at the server-local today.</summary>
    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > today)
            age--;

        return age;
    }
}
