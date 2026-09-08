using Application.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Identity;

/// <summary>
/// Localizes ASP.NET Identity error descriptions (registration, password
/// change/reset) through the shared resx instead of the framework English
/// texts. Unmapped codes fall back to the base (English) implementation.
/// </summary>
public sealed class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public override IdentityError PasswordMismatch() => New("IdentityPasswordMismatch");

    public override IdentityError PasswordTooShort(int length) => New("IdentityPasswordTooShort");

    public override IdentityError PasswordRequiresDigit() => New("IdentityPasswordRequiresDigit");

    public override IdentityError PasswordRequiresLower() => New("IdentityPasswordRequiresLower");

    public override IdentityError PasswordRequiresUpper() => New("IdentityPasswordRequiresUpper");

    public override IdentityError PasswordRequiresNonAlphanumeric() => New("IdentityPasswordRequiresNonAlphanumeric");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => New("IdentityPasswordRequiresUniqueChars");

    public override IdentityError DuplicateUserName(string userName) => New("IdentityDuplicateUser");

    public override IdentityError DuplicateEmail(string email) => New("IdentityDuplicateUser");

    public override IdentityError InvalidToken() => New("IdentityInvalidToken");

    private IdentityError New(string key, params object[] args)
        => new() { Code = key, Description = _localizer[key, args].Value };
}
