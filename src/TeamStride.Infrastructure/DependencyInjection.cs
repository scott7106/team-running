using Microsoft.Extensions.DependencyInjection;
using TeamStride.Application.Athletes.Services;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Services;
using TeamStride.Application.Users.Services;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(new[]
            {
                typeof(DependencyInjection).Assembly,    // Infrastructure assembly
                typeof(Application.Users.Dtos.UserDto).Assembly  // Application assembly
            });
        });

        // Register team services
        services.AddScoped<IStandardTeamService, StandardTeamService>();
        services.AddScoped<IGlobalAdminTeamService, GlobalAdminTeamService>();
        services.AddScoped<ITenantSwitcherService, TenantSwitcherService>();

        // Register user services
        services.AddScoped<IGlobalAdminUserService, GlobalAdminUserService>();

        // Register athlete services
        services.AddScoped<IAthleteService, AthleteService>();

        // Register dashboard services
        services.AddScoped<IGlobalAdminDashboardService, GlobalAdminDashboardService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    { 
        return services;
    }
} 