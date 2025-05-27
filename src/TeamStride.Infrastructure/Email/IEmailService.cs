using System.Threading.Tasks;

namespace TeamStride.Infrastructure.Email;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
    Task SendEmailConfirmationAsync(string email, string confirmationLink);
    Task SendPasswordResetAsync(string email, string resetLink);
    Task SendOwnershipTransferAsync(string email, string teamName, string initiatedByName, string transferLink, string? message = null);
} 