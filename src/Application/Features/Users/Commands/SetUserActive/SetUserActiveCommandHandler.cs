using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Users.Commands;

public sealed class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand, Result>
{
    private readonly IUserManager _users;

    public SetUserActiveCommandHandler(IUserManager users)
    {
        _users = users;
    }

    public async Task<Result> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var account = await _users.FindAsync(request.Id, cancellationToken);
        if (account is null)
            return Result.Failure($"Resource 'Object' with id '{request.Id}' was not found.", 404);

        var result = await _users.SetActiveAsync(request.Id, request.Request.IsActive, cancellationToken);

        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors), 409);

        return Result.Success();
    }
}