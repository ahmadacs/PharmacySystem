using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Auth.Dtos;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Auth.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserManager _users;
    private readonly ITokenService _tokens;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LoginCommandHandler(IUserManager users, ITokenService tokens, IStringLocalizer<SharedResource> localizer)
    {
        _users = users;
        _tokens = tokens;
        _localizer = localizer;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var account = await _users.FindByEmailAsync(req.Email, cancellationToken);
        if (account is null)
            return Result<AuthResponse>.Failure(_localizer["InvalidCredentials"].Value, 401);

        var check = await _users.CheckPasswordAsync(req.Email, req.Password, cancellationToken);

        switch (check)
        {
            case PasswordCheckResult.LockedOut:
                return Result<AuthResponse>.Failure(_localizer["InvalidCredentials"].Value, 401);
            case PasswordCheckResult.Failed:
            case PasswordCheckResult.NotAllowed:
                return Result<AuthResponse>.Failure(_localizer["InvalidCredentials"].Value, 401);
        }

        if (!account.IsActive)
            return Result<AuthResponse>.Failure(_localizer["InvalidCredentials"].Value, 401);

        var tokens = await _tokens.CreateAsync(account.Id, cancellationToken);
        return Result<AuthResponse>.Success(AuthMapping.ToResponse(tokens, account));
    }
}