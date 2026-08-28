using System.ComponentModel.DataAnnotations;

namespace Application.Features.Auth.Dtos;

public sealed record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record RegisterRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter and one digit.")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>Doctor or Pharmacist — Admin accounts are managed by an administrator.</summary>
    [Required]
    public string Role { get; init; } = string.Empty;

    [Required, StringLength(20)]
    public string LicenseNumber { get; init; } = string.Empty;

    [StringLength(150)]
    public string? Specialization { get; init; }

    [StringLength(30)]
    public string? PhoneNumber { get; init; }
}

public sealed record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter and one digit.")]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed record ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public sealed record ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter and one digit.")]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; init; } = string.Empty;
}