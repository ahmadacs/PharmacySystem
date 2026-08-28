using Application.Common.Interfaces;
using Application.Features.Auth.Dtos;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserManager _users;
    private readonly ITokenService _tokens;

    public LoginCommandHandler(IUserManager users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var account = await _users.FindByEmailAsync(req.Email, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var check = await _users.CheckPasswordAsync(req.Email, req.Password, cancellationToken);

        switch (check)
        {
            case PasswordCheckResult.LockedOut:
                throw new AccountLockedOutException();
            case PasswordCheckResult.Failed:
            case PasswordCheckResult.NotAllowed:
                throw new InvalidCredentialsException();
        }

        if (!account.IsActive)
            throw new AccountDisabledException();

        var tokens = await _tokens.CreateAsync(account.Id, cancellationToken);
        return AuthMapping.ToResponse(tokens, account);
    }
}