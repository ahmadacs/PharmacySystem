using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUserManager _users;

    public ResetPasswordCommandHandler(IUserManager users)
    {
        _users = users;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _users.ResetPasswordAsync(request.Request.Email, request.Request.Token, request.Request.NewPassword, cancellationToken);

        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors), 401);

        return Result.Success();
    }
}