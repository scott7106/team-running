using Microsoft.Extensions.DependencyInjection;
using TeamStride.Application.Athletes.Services;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAthleteService, AthleteService>();
        
        return services;
    }
} 