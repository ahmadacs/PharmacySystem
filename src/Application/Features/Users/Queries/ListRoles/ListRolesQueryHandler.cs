using Application.Common.Security;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Roles.All);
}