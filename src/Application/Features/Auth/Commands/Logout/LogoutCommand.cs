using MediatR;

namespace Application.Features.Auth.Commands;

public sealed record LogoutCommand(string? RefreshToken) : IRequest;