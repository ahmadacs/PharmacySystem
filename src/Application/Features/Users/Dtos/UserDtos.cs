using Application.Common.Interfaces;

namespace Application.Features.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string? FullName,
    bool IsActive,
    IReadOnlyList<string> Roles);

public static class UserMapping
{
    public static UserDto ToDto(this UserAccount account)
        => new(account.Id, account.Email, account.FullName, account.IsActive, account.Roles);
}