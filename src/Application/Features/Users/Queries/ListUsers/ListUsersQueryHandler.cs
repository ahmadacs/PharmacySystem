using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedList<UserDto>>
{
    private readonly IUserManager _users;

    public ListUsersQueryHandler(IUserManager users)
    {
        _users = users;
    }

    public async Task<PagedList<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
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

        return new PagedList<UserDto>
        {
            Items = result.Items
                .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.IsActive, u.Roles))
                .ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}