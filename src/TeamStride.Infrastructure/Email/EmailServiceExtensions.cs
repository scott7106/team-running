using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace TeamStride.Infrastructure.Email;

public static class EmailServiceExtensions
{
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        if (isDevelopment)
        {
            services.AddScoped<IEmailService, NullEmailService>();
        }
        else
        {
            var sendGridConfig = configuration.GetSection("SendGrid").Get<SendGridConfiguration>();
            if (sendGridConfig == null)
            {
                throw new InvalidOperationException("SendGrid configuration is missing");
            }
            services.AddScoped<IEmailService>(sp => new SendGridEmailService(
                sendGridConfig.ApiKey,
                sendGridConfig.FromEmail,
                sendGridConfig.FromName));
        }

        return services;
    }
}

public class SendGridConfiguration
{
    public required string ApiKey { get; set; }
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
} 