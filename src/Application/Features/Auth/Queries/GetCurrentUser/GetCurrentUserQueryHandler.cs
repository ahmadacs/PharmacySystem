using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Auth.Dtos;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Auth.Queries;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserManager _users;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, IUserManager users, IStringLocalizer<SharedResource> localizer)
    {
        _currentUser = currentUser;
        _users = users;
        _localizer = localizer;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CurrentUserDto>.Failure(_localizer["Forbidden"].Value, 403);

        var account = await _users.FindAsync(_currentUser.UserId.Value, cancellationToken);
        if (account is null)
            return Result<CurrentUserDto>.Failure(_localizer["EmailOrPasswordIncorrect"].Value, 401);

        return Result<CurrentUserDto>.Success(account.ToDto());
    }
}