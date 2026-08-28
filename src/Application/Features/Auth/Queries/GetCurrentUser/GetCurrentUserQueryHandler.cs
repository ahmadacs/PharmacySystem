using Application.Common.Interfaces;
using Application.Features.Auth.Dtos;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Auth.Queries;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserManager _users;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, IUserManager users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenResourceException();

        var account = await _users.FindAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new InvalidCredentialsException();

        return new CurrentUserDto(
            account.Id,
            account.Email,
            account.FullName,
            account.Roles.FirstOrDefault(),
            account.Permissions);
    }
}