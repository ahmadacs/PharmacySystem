using Application.Common.Models;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed record ListRolesQuery : IRequest<Result<IReadOnlyList<string>>>;