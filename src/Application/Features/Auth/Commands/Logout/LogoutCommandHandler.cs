using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ITokenService _tokens;

    public LogoutCommandHandler(ITokenService tokens)
    {
        _tokens = tokens;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await _tokens.RevokeAsync(request.RefreshToken, cancellationToken);

        return Result.Success();
    }
}