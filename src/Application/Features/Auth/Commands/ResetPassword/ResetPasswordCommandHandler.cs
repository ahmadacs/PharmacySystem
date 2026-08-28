using Application.Common.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserManager _users;

    public ResetPasswordCommandHandler(IUserManager users)
    {
        _users = users;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _users.ResetPasswordAsync(request.Request.Email, request.Request.Token, request.Request.NewPassword, cancellationToken);

        if (!result.Succeeded)
            throw new InvalidCredentialsException(string.Join("; ", result.Errors));
    }
}