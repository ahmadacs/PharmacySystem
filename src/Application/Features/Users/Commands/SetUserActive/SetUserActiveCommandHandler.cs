using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Users.Commands;

public sealed class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand, Result>
{
    private readonly IUserManager _users;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SetUserActiveCommandHandler(IUserManager users, IStringLocalizer<SharedResource> localizer)
    {
        _users = users;
        _localizer = localizer;
    }

    public async Task<Result> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var account = await _users.FindAsync(request.Id, cancellationToken);
        if (account is null)
            return Result.Failure(_localizer["ResourceNotFound", "User", request.Id.ToString()].Value, 404);

        var result = await _users.SetActiveAsync(request.Id, request.Request.IsActive, cancellationToken);

        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors), 409);

        return Result.Success();
    }
}