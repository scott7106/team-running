using System.Threading.Tasks;
using TeamStride.Application.Teams.Dtos;

namespace TeamStride.Application.Common.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, string? from = null, bool isHtml = true, params (string Email, string Name)[] recipients);

    // Registration-specific methods
    Task SendRegistrationConfirmationAsync(TeamRegistrationDto registration);
    Task SendRegistrationStatusUpdateAsync(TeamRegistrationDto registration);

    Task SendEmailConfirmationAsync(string to, string confirmationLink);
    Task SendPasswordResetAsync(string to, string resetLink);
} 