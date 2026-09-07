using Application.Common.Interfaces;

namespace Application.Features.Users.Dtos;

public static class UserMapping
{
    public static UserDto ToDto(this UserAccount account)
        => new(account.Id, account.Email, account.FullName, account.IsActive, account.Roles);
}
