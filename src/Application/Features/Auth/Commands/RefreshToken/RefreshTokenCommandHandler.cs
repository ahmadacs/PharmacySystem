using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly ITokenService _tokens;
    private readonly IUserManager _users;

    public RefreshTokenCommandHandler(ITokenService tokens, IUserManager users)
    {
        _tokens = tokens;
        _users = users;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<AuthResponse>.Failure("The refresh token is invalid, expired or has been revoked.", 401);

        var tokens = await _tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        var account = await _users.FindAsync(tokens.UserId, cancellationToken);
        if (account is null)
            return Result<AuthResponse>.Failure("The refresh token is invalid, expired or has been revoked.", 401);

        if (!account.IsActive)
            return Result<AuthResponse>.Failure("This account has been disabled. Contact an administrator.", 401);

        return Result<AuthResponse>.Success(AuthMapping.ToResponse(tokens, account));
    }
}