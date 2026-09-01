using Application.Common.Models;
using Application.Common.Security;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, Result<IReadOnlyList<string>>>
{
    public Task<Result<IReadOnlyList<string>>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<IReadOnlyList<string>>.Success(Roles.All));
    }
}