using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGlobalAdminSeeder(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GlobalAdminConfiguration>(
            configuration.GetSection("GlobalAdmin"));

        services.AddTransient<GlobalAdminDataSeeder>();
        services.AddHostedService<GlobalAdminSeederHostedService>();

        return services;
    }

    public static IServiceCollection AddApplicationRoleSeeder(
        this IServiceCollection services)
    {
        services.AddTransient<ApplicationRoleSeeder>();
        services.AddHostedService<ApplicationRoleSeederHostedService>();

        return services;
    }
} 