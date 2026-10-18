using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Commands;

public sealed record UpdateRolePermissionsCommand(string Role, UpdateRolePermissionsRequest Request) : IRequest<Result>;
