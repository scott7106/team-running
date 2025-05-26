using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace TeamStride.Infrastructure.Email;

/// <summary>
/// A development-only email service that logs email content instead of sending actual emails.
/// This service should only be used in development environments.
/// </summary>
public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;

    public NullEmailService(ILogger<NullEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Email would have been sent:
            To: {To}
            Subject: {Subject}
            Content:
            {Content}
            """,
            to, subject, htmlContent);

        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Email confirmation would have been sent:
            To: {Email}
            Confirmation Link: {Link}
            """,
            email, confirmationLink);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetLink)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Password reset email would have been sent:
            To: {Email}
            Reset Link: {Link}
            """,
            email, resetLink);

        return Task.CompletedTask;
    }
} 