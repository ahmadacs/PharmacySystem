using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed record GetRolePermissionsQuery(string Role) : IRequest<Result<RolePermissionsDto>>;
