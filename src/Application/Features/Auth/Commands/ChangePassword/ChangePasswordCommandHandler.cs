using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Auth.Commands;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserManager _users;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ChangePasswordCommandHandler(ICurrentUserService currentUser, IUserManager users, IStringLocalizer<SharedResource> localizer)
    {
        _currentUser = currentUser;
        _users = users;
        _localizer = localizer;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Failure(_localizer["Forbidden"].Value, 403);

        var result = await _users.ChangePasswordAsync(
            _currentUser.UserId.Value,
            request.Request.CurrentPassword,
            request.Request.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors), 401);

        return Result.Success();
    }
}