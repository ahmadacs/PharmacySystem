using Application.Common.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserManager _users;

    public ChangePasswordCommandHandler(ICurrentUserService currentUser, IUserManager users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenResourceException();

        var result = await _users.ChangePasswordAsync(
            _currentUser.UserId.Value,
            request.Request.CurrentPassword,
            request.Request.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
            throw new InvalidCredentialsException(string.Join("; ", result.Errors));
    }
}