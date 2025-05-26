using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamStride.Application.Authentication.Services;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Identity;

namespace TeamStride.Infrastructure.Identity;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddTeamStrideIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<IdentityContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("TeamStride.Infrastructure")));

        // Register Identity services
        services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<IdentityContext>()
        .AddDefaultTokenProviders();

        // Register authentication configuration
        var authConfig = configuration.GetSection("Authentication").Get<AuthenticationConfiguration>();
        if (authConfig == null)
        {
            throw new InvalidOperationException("Authentication configuration is missing");
        }

        services.AddSingleton(authConfig);

        // Register services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TeamStride.Application.Authentication.Services.IAuthenticationService, AuthenticationService>();
        return services;
    }
} 