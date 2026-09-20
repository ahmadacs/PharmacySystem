using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, Result<PagedList<UserDto>>>
{
    private readonly IUserManager _users;

    public ListUsersQueryHandler(IUserManager users)
    {
        _users = users;
    }

    public async Task<Result<PagedList<UserDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _users.ListAsync(
            request.Search,
            request.Role,
            request.IsActive,
            request.SortBy,
            request.SortDir,
            request.Page,
            request.PageSize,
            cancellationToken);

        var items = result.Items
            .Select(u => u.ToDto())
            .ToPagedList(result.Page, result.PageSize, result.TotalCount);

        return Result<PagedList<UserDto>>.Success(items);
    }
}