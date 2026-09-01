using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserManager _users;
    private readonly ITokenService _tokens;

    public LoginCommandHandler(IUserManager users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var account = await _users.FindByEmailAsync(req.Email, cancellationToken);
        if (account is null)
            return Result<AuthResponse>.Failure("Invalid credentials.", 401);

        var check = await _users.CheckPasswordAsync(req.Email, req.Password, cancellationToken);

        switch (check)
        {
            case PasswordCheckResult.LockedOut:
                return Result<AuthResponse>.Failure("Invalid credentials.", 401);
            case PasswordCheckResult.Failed:
            case PasswordCheckResult.NotAllowed:
                return Result<AuthResponse>.Failure("Invalid credentials.", 401);
        }

        if (!account.IsActive)
            return Result<AuthResponse>.Failure("Invalid credentials.", 401);

        var tokens = await _tokens.CreateAsync(account.Id, cancellationToken);
        return Result<AuthResponse>.Success(AuthMapping.ToResponse(tokens, account));
    }
}