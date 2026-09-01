using Application.Common.Models;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;