using Application.Common.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Users.Commands;

public sealed class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IUserManager _users;

    public SetUserActiveCommandHandler(IUserManager users)
    {
        _users = users;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var account = await _users.FindAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(object), request.Id);

        var result = await _users.SetActiveAsync(request.Id, request.Request.IsActive, cancellationToken);

        if (!result.Succeeded)
            throw new ConflictingOperationException(string.Join("; ", result.Errors));
    }
}