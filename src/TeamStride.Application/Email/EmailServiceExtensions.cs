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
    public string ApiKey { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
} 