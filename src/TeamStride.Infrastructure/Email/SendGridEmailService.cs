using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace TeamStride.Infrastructure.Email;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(string apiKey, string fromEmail, string fromName)
    {
        _sendGridClient = new SendGridClient(apiKey);
        _fromEmail = fromEmail;
        _fromName = fromName;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        var from = new EmailAddress(_fromEmail, _fromName);
        var toAddress = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlContent);
        await _sendGridClient.SendEmailAsync(msg);
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        var subject = "Confirm your TeamStride account";
        var htmlContent = $@"
            <h2>Welcome to TeamStride!</h2>
            <p>Please confirm your account by clicking the link below:</p>
            <p><a href='{confirmationLink}'>Confirm Email</a></p>
            <p>If you did not create this account, please ignore this email.</p>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async Task SendPasswordResetAsync(string email, string resetLink)
    {
        var subject = "Reset your TeamStride password";
        var htmlContent = $@"
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password. Click the link below to proceed:</p>
            <p><a href='{resetLink}'>Reset Password</a></p>
            <p>If you did not request this password reset, please ignore this email.</p>
            <p>This link will expire in 24 hours.</p>";

        await SendEmailAsync(email, subject, htmlContent);
    }
} 