using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Auth.Dtos;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Auth.Commands;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUserManager _users;
    private readonly IStaffService _staff;
    private readonly ITokenService _tokens;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RegisterCommandHandler(IUserManager users, IStaffService staff, ITokenService tokens, IStringLocalizer<SharedResource> localizer)
    {
        _users = users;
        _staff = staff;
        _tokens = tokens;
        _localizer = localizer;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var role = req.Role.Trim();
        if (role != Roles.Doctor && role != Roles.Pharmacist)
            return Result<AuthResponse>.Failure(_localizer["SelfRegistration"].Value, 409);

        var result = await _users.TryCreateUserAsync(
            req.Email,
            req.FirstName,
            req.LastName,
            req.Password,
            [role],
            cancellationToken);

        if (result.UserId is null)
            return Result<AuthResponse>.Failure(_localizer["RegisterFailed", string.Join("; ", result.Errors)].Value, 409);

        var userId = result.UserId.Value;

        if (role == Roles.Doctor)
        {
            await _staff.CreateDoctorProfileAsync(userId, req.LicenseNumber, req.Specialization, req.PhoneNumber, cancellationToken);
        }
        else
        {
            await _staff.CreatePharmacistProfileAsync(userId, req.LicenseNumber, cancellationToken);
        }

        var tokens = await _tokens.CreateAsync(userId, cancellationToken);
        var account = await _users.FindAsync(userId, cancellationToken);
        if (account is null)
            return Result<AuthResponse>.Failure(_localizer["EmailOrPasswordIncorrect"].Value, 401);

        return Result<AuthResponse>.Success(AuthMapping.ToResponse(tokens, account));
    }
}