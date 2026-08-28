using System.ComponentModel.DataAnnotations;

namespace Application.Features.Users.Dtos;

public sealed record CreateUserRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter and one digit.")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = string.Empty;

    [StringLength(20)]
    public string? LicenseNumber { get; init; }

    [StringLength(150)]
    public string? Specialization { get; init; }

    [StringLength(30)]
    public string? PhoneNumber { get; init; }
}

public sealed record SetUserActiveRequest
{
    public bool IsActive { get; init; }
}