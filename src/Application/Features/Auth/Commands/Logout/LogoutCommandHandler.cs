using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly ITokenService _tokens;

    public LogoutCommandHandler(ITokenService tokens)
    {
        _tokens = tokens;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await _tokens.RevokeAsync(request.RefreshToken, cancellationToken);
    }
}