using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserManager _users;

    public ChangePasswordCommandHandler(ICurrentUserService currentUser, IUserManager users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Failure("You are not allowed to access this resource.", 403);

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