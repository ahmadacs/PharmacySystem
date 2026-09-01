using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Commands;

public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<Result<Guid>>;