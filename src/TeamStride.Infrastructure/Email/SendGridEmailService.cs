using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;

namespace TeamStride.Infrastructure.Email;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(
        ISendGridClient sendGridClient,
        ILogger<SendGridEmailService> logger,
        string fromEmail,
        string fromName)
    {
        _sendGridClient = sendGridClient;
        _logger = logger;
        _fromEmail = fromEmail;
        _fromName = fromName;
    }

    public async Task SendEmailAsync(string to, string subject, string body, string? from = null, bool isHtml = true, params (string Email, string Name)[] recipients)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(from ?? _fromEmail, _fromName),
            Subject = subject,
            HtmlContent = isHtml ? body : null,
            PlainTextContent = isHtml ? null : body
        };
        msg.AddTo(new EmailAddress(to));
        if (recipients != null)
        {
            foreach (var (Email, Name) in recipients)
            {
                msg.AddCc(new EmailAddress(Email, Name));
            }
        }
        await _sendGridClient.SendEmailAsync(msg);
    }

    public async Task SendEmailConfirmationAsync(string to, string confirmationLink)
    {
        var subject = "Confirm your email address";
        var htmlContent = $@"
            <h2>Welcome to TeamStride!</h2>
            <p>Please confirm your email address by clicking the link below:</p>
            <p><a href='{confirmationLink}'>Confirm Email</a></p>
            <p>If you did not create an account, you can safely ignore this email.</p>";

        await SendEmailAsync(to, subject, htmlContent);
    }

    public async Task SendPasswordResetAsync(string to, string resetLink)
    {
        var subject = "Reset your password";
        var htmlContent = $@"
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password. Click the link below to proceed:</p>
            <p><a href='{resetLink}'>Reset Password</a></p>
            <p>If you did not request a password reset, you can safely ignore this email.</p>
            <p>This link will expire in 1 hour.</p>";

        await SendEmailAsync(to, subject, htmlContent);
    }

    public async Task SendOwnershipTransferAsync(string to, string teamName, string transferLink)
    {
        var subject = $"Team Ownership Transfer: {teamName}";
        var htmlContent = $@"
            <h2>Team Ownership Transfer</h2>
            <p>A team ownership transfer has been initiated for {teamName}.</p>
            <p>Click the link below to accept the transfer:</p>
            <p><a href='{transferLink}'>Accept Ownership Transfer</a></p>
            <p>If you do not wish to accept this transfer, you can safely ignore this email.</p>
            <p>This link will expire in 7 days.</p>";

        await SendEmailAsync(to, subject, htmlContent);
    }

    public async Task SendRegistrationConfirmationAsync(TeamRegistrationDto registration)
    {
        var subject = $"Registration Confirmation - {registration.TeamName}";
        var htmlContent = $@"
            <h2>Registration Confirmation</h2>
            <p>Thank you for registering with {registration.TeamName}!</p>
            <p>Registration Details:</p>
            <ul>
                <li>Team: {registration.TeamName}</li>
                <li>Registration Date: {registration.CreatedOn:g}</li>
                <li>Status: {registration.Status}</li>
            </ul>
            <p>Registered Athletes:</p>
            <ul>
                {string.Join("", registration.Athletes.Select(a => $"<li>{a.FirstName} {a.LastName} - Grade {a.GradeLevel}</li>"))}
            </ul>
            <p>We will review your registration and contact you with further instructions.</p>";

        await SendEmailAsync(registration.Email, subject, htmlContent);
    }

    public async Task SendRegistrationStatusUpdateAsync(TeamRegistrationDto registration)
    {
        var subject = $"Registration Status Update - {registration.TeamName}";
        var htmlContent = $@"
            <h2>Registration Status Update</h2>
            <p>Your registration status for {registration.TeamName} has been updated.</p>
            <p>Registration Details:</p>
            <ul>
                <li>Team: {registration.TeamName}</li>
                <li>Registration Date: {registration.CreatedOn:g}</li>
                <li>Status: {registration.Status}</li>
            </ul>
            <p>Registered Athletes:</p>
            <ul>
                {string.Join("", registration.Athletes.Select(a => $"<li>{a.FirstName} {a.LastName} - Grade {a.GradeLevel}</li>"))}
            </ul>
            {GetStatusSpecificMessage(registration.Status)}";

        await SendEmailAsync(registration.Email, subject, htmlContent);
    }

    private string GetStatusSpecificMessage(RegistrationStatus status)
    {
        return status switch
        {
            RegistrationStatus.Approved => @"
                <p>Your registration has been approved! You can now access the team portal and complete your athlete profiles.</p>
                <p>Next steps:</p>
                <ol>
                    <li>Log in to your account</li>
                    <li>Complete athlete profiles</li>
                    <li>Review team information</li>
                </ol>",
            RegistrationStatus.Waitlisted => @"
                <p>Your registration has been placed on the waitlist. We will contact you if a spot becomes available.</p>
                <p>You will be notified by email if your status changes.</p>",
            RegistrationStatus.Rejected => @"
                <p>We regret to inform you that your registration has not been approved at this time.</p>
                <p>If you have any questions, please contact the team administrator.</p>",
            _ => ""
        };
    }
} 