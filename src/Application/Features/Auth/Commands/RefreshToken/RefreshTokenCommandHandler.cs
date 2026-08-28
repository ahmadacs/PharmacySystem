using Application.Common.Interfaces;
using Application.Features.Auth.Dtos;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly ITokenService _tokens;
    private readonly IUserManager _users;

    public RefreshTokenCommandHandler(ITokenService tokens, IUserManager users)
    {
        _tokens = tokens;
        _users = users;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new InvalidRefreshTokenException();

        var tokens = await _tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        var account = await _users.FindAsync(tokens.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        if (!account.IsActive)
            throw new AccountDisabledException();

        return AuthMapping.ToResponse(tokens, account);
    }
}