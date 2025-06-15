using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;

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

    public Task SendEmailAsync(string to, string subject, string body, string? from = null, bool isHtml = true, params (string Email, string Name)[] recipients)
        => Task.CompletedTask;

    public Task SendEmailConfirmationAsync(string to, string confirmationLink)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Email confirmation would have been sent:
            To: {Email}
            Confirmation Link: {Link}
            """,
            to, confirmationLink);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string to, string resetLink)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Password reset email would have been sent:
            To: {Email}
            Reset Link: {Link}
            """,
            to, resetLink);

        return Task.CompletedTask;
    }

    public Task SendOwnershipTransferAsync(string to, string teamName, string transferLink)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Ownership transfer email would have been sent:
            To: {Email}
            Team: {TeamName}
            Transfer Link: {Link}
            """,
            to, teamName, transferLink);

        return Task.CompletedTask;
    }

    public Task SendRegistrationConfirmationAsync(TeamRegistrationDto registration)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Registration confirmation email would have been sent:
            To: {Email}
            Name: {FirstName} {LastName}
            Emergency Contact: {EmergencyContactName} ({EmergencyContactPhone})
            Athletes:
            {Athletes}
            """,
            registration.Email,
            registration.FirstName,
            registration.LastName,
            registration.EmergencyContactName,
            registration.EmergencyContactPhone,
            string.Join("\n", registration.Athletes.Select(a => $"- {a.FirstName} {a.LastName} (Grade: {a.GradeLevel})")));

        return Task.CompletedTask;
    }

    public Task SendRegistrationStatusUpdateAsync(TeamRegistrationDto registration)
    {
        _logger.LogInformation(
            """
            [DEV ONLY] Registration status update email would have been sent:
            To: {Email}
            Name: {FirstName} {LastName}
            Status: {Status}
            Emergency Contact: {EmergencyContactName} ({EmergencyContactPhone})
            Athletes:
            {Athletes}
            """,
            registration.Email,
            registration.FirstName,
            registration.LastName,
            registration.Status,
            registration.EmergencyContactName,
            registration.EmergencyContactPhone,
            string.Join("\n", registration.Athletes.Select(a => $"- {a.FirstName} {a.LastName} (Grade: {a.GradeLevel})")));

        return Task.CompletedTask;
    }
} 