using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Queries;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserManager _users;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, IUserManager users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CurrentUserDto>.Failure("You are not allowed to access this resource.", 403);

        var account = await _users.FindAsync(_currentUser.UserId.Value, cancellationToken);
        if (account is null)
            return Result<CurrentUserDto>.Failure("The email or password is incorrect.", 401);

        return Result<CurrentUserDto>.Success(new CurrentUserDto(
            account.Id,
            account.Email,
            account.FullName,
            account.Roles.FirstOrDefault(),
            account.Permissions));
    }
}