using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using TeamStride.Application.Common.Services;

namespace TeamStride.Infrastructure.Email;

public static class EmailServiceExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        var emailSettings = configuration.GetSection("Email").Get<EmailSettings>();
        if (emailSettings == null)
        {
            services.AddScoped<IEmailService, NullEmailService>();
            return services;
        }

        services.AddSingleton<ISendGridClient>(new SendGridClient(emailSettings.SendGridApiKey));
        services.AddScoped<IEmailService>(sp =>
        {
            var client = sp.GetRequiredService<ISendGridClient>();
            var logger = sp.GetRequiredService<ILogger<SendGridEmailService>>();
            return new SendGridEmailService(client, logger, emailSettings.FromEmail, emailSettings.FromName);
        });

        return services;
    }
}

public class EmailSettings
{
    public string SendGridApiKey { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
} 