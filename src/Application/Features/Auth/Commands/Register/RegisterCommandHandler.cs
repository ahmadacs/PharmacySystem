using Application.Common.Interfaces;
using Application.Common.Security;
using Application.Features.Auth.Dtos;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserManager _users;
    private readonly IStaffService _staff;
    private readonly ITokenService _tokens;

    public RegisterCommandHandler(IUserManager users, IStaffService staff, ITokenService tokens)
    {
        _users = users;
        _staff = staff;
        _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var role = req.Role.Trim();
        if (role != Roles.Doctor && role != Roles.Pharmacist)
            throw new ConflictingOperationException("Self-registration is only available for Doctor and Pharmacist accounts.");

        var result = await _users.TryCreateUserAsync(
            req.Email,
            req.FirstName,
            req.LastName,
            req.Password,
            [role],
            cancellationToken);

        if (result.UserId is null)
            throw new ConflictingOperationException($"Unable to register the account: {string.Join("; ", result.Errors)}");

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
        var account = await _users.FindAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        return AuthMapping.ToResponse(tokens, account);
    }
}