using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Resources;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Users.Commands;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IUserManager _users;
    private readonly IStaffService _staff;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateUserCommandHandler(IUserManager users, IStaffService staff, IStringLocalizer<SharedResource> localizer)
    {
        _users = users;
        _staff = staff;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var role = req.Role.Trim();
        if (!Roles.All.Contains(role))
            return Result<Guid>.Failure(_localizer["UnknownRole", role].Value, 409);

        var result = await _users.TryCreateUserAsync(
            req.Email,
            req.FirstName,
            req.LastName,
            req.Password,
            [role],
            cancellationToken);

        if (result.UserId is null)
            return Result<Guid>.Failure(_localizer["CreateUserFailed", string.Join("; ", result.Errors)].Value, 409);

        var userId = result.UserId.Value;

        if (role == Roles.Doctor)
        {
            await _staff.CreateDoctorProfileAsync(userId, req.LicenseNumber ?? string.Empty, req.Specialization, req.PhoneNumber, cancellationToken);
        }
        else if (role == Roles.Pharmacist)
        {
            await _staff.CreatePharmacistProfileAsync(userId, req.LicenseNumber ?? string.Empty, cancellationToken);
        }

        return Result<Guid>.Success(userId);
    }
}