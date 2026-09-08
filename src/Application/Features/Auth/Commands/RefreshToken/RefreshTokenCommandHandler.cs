using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Auth.Dtos;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Auth.Commands;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly ITokenService _tokens;
    private readonly IUserManager _users;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RefreshTokenCommandHandler(ITokenService tokens, IUserManager users, IStringLocalizer<SharedResource> localizer)
    {
        _tokens = tokens;
        _users = users;
        _localizer = localizer;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<AuthResponse>.Failure(_localizer["RefreshTokenInvalid"].Value, 401);

        var tokens = await _tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        var account = await _users.FindAsync(tokens.UserId, cancellationToken);
        if (account is null)
            return Result<AuthResponse>.Failure(_localizer["RefreshTokenInvalid"].Value, 401);

        if (!account.IsActive)
            return Result<AuthResponse>.Failure(_localizer["AccountDisabled"].Value, 401);

        return Result<AuthResponse>.Success(AuthMapping.ToResponse(tokens, account));
    }
}