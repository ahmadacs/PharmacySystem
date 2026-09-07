using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Users.Dtos;
using MediatR;

namespace Application.Features.Users.Queries;

public sealed record ListUsersQuery : PagedQuery, IRequest<PagedList<UserDto>>
{
    [StringLength(50, ErrorMessage = "Role must be at most 50 characters.")]
    public string? Role { get; init; }

    public bool? IsActive { get; init; }

    public override string? SortBy { get; init; } = "email";

    public override string SortDir { get; init; } = "asc";
}
