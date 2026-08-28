using MediatR;

namespace Application.Features.Users.Queries;

public sealed record ListRolesQuery : IRequest<IReadOnlyList<string>>;