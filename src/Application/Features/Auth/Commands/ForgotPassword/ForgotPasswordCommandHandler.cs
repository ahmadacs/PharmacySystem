using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Commands;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUserManager _users;
    private readonly IEmailService _emails;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(IUserManager users, IEmailService emails, ILogger<ForgotPasswordCommandHandler> logger)
    {
        _users = users;
        _emails = emails;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {

        var token = await _users.GeneratePasswordResetTokenAsync(request.Request.Email, cancellationToken);
        if (token is null)
            return Result.Success();

        _logger.LogInformation("Password reset requested for {Email}. Token: {Token}", request.Request.Email, token);
        await _emails.SendAsync(request.Request.Email, "Password reset", $"Your password reset token is: {token}", cancellationToken);

        return Result.Success();
    }
}