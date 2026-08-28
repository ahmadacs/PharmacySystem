using Application.Common.Interfaces;
using Application.Common.Security;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Users.Commands;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserManager _users;
    private readonly IStaffService _staff;

    public CreateUserCommandHandler(IUserManager users, IStaffService staff)
    {
        _users = users;
        _staff = staff;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var role = req.Role.Trim();
        if (!Roles.All.Contains(role))
            throw new ConflictingOperationException($"Unknown role '{role}'.");

        var result = await _users.TryCreateUserAsync(
            req.Email,
            req.FirstName,
            req.LastName,
            req.Password,
            [role],
            cancellationToken);

        if (result.UserId is null)
            throw new ConflictingOperationException($"Unable to create the user: {string.Join("; ", result.Errors)}");

        var userId = result.UserId.Value;

        if (role == Roles.Doctor)
        {
            await _staff.CreateDoctorProfileAsync(userId, req.LicenseNumber ?? string.Empty, req.Specialization, req.PhoneNumber, cancellationToken);
        }
        else if (role == Roles.Pharmacist)
        {
            await _staff.CreatePharmacistProfileAsync(userId, req.LicenseNumber ?? string.Empty, cancellationToken);
        }

        return userId;
    }
}