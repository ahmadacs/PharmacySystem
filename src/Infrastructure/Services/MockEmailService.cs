using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Mock email gateway: instead of sending mail, every message is written to the
/// application log so password-reset flows can be reviewed end to end while we
/// are not hosting a real SMTP server.
/// </summary>
public sealed class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MOCK EMAIL -> {To} | Subject: {Subject} | Body:\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}