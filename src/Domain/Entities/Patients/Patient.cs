using Domain.Common;
using Domain.Entities.Prescriptions;

namespace Domain.Entities.Patients;

public class Patient : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;

    private readonly List<Prescription> _prescriptions = new();
    public IReadOnlyCollection<Prescription> Prescriptions => _prescriptions.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}".Trim();

    public int Age
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.AddYears(age) > today)
                age--;

            return age;
        }
    }

    private Patient() { }

    public Patient(string firstName, string lastName, DateOnly dateOfBirth, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        if (dateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Date of birth cannot be in the future.", nameof(dateOfBirth));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        PhoneNumber = phoneNumber.Trim().Replace(" ", "").Replace("-", "");
    }

    public void UpdatePhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        PhoneNumber = phoneNumber.Trim().Replace(" ", "").Replace("-", "");
    }
}