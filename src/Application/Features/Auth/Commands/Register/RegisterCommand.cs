using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;