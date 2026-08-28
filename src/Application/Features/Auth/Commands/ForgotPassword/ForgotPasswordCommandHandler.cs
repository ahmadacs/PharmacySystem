using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Commands;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
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

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Always return success regardless of whether the email exists so we do
        // not leak which addresses are registered.
        var token = await _users.GeneratePasswordResetTokenAsync(request.Request.Email, cancellationToken);
        if (token is null)
            return;

        // In a real deployment the token would be embedded in a reset link and
        // emailed. Here the email is mocked to the log for reviewers.
        _logger.LogInformation("Password reset requested for {Email}. Token: {Token}", request.Request.Email, token);
        await _emails.SendAsync(request.Request.Email, "Password reset", $"Your password reset token is: {token}", cancellationToken);
    }
}