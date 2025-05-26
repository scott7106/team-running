using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using TeamStride.Application.Authentication.Services;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Identity;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamStrideIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication").Get<AuthenticationConfiguration>();
        
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<IdentityContext>()
        .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authConfig.JwtIssuer,
                ValidAudience = authConfig.JwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(authConfig.JwtSecret))
            };
        })
        .AddOAuthProviders(authConfig)
        .AddMicrosoftAccount(options =>
        {
            options.ClientId = authConfig.Microsoft.ClientId;
            options.ClientSecret = authConfig.Microsoft.ClientSecret;
        })
        .AddGoogle(options =>
        {
            options.ClientId = authConfig.Google.ClientId;
            options.ClientSecret = authConfig.Google.ClientSecret;
        })
        .AddFacebook(options =>
        {
            options.ClientId = authConfig.Facebook.ClientId;
            options.ClientSecret = authConfig.Facebook.ClientSecret;
        })
        .AddTwitter(options =>
        {
            options.ConsumerKey = authConfig.Twitter.ClientId;
            options.ConsumerSecret = authConfig.Twitter.ClientSecret;
        });

        // Register HttpClient and ExternalAuthService
        services.AddHttpClient();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();

        return services;
    }

    public static AuthenticationBuilder AddOAuthProviders(this AuthenticationBuilder builder, AuthenticationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(config.Microsoft);
        ArgumentNullException.ThrowIfNull(config.Google);
        ArgumentNullException.ThrowIfNull(config.Facebook);
        ArgumentNullException.ThrowIfNull(config.Twitter);

        builder
            .AddMicrosoftAccount(options =>
            {
                options.ClientId = config.Microsoft.ClientId;
                options.ClientSecret = config.Microsoft.ClientSecret;
            })
            .AddGoogle(options =>
            {
                options.ClientId = config.Google.ClientId;
                options.ClientSecret = config.Google.ClientSecret;
            })
            .AddFacebook(options =>
            {
                options.ClientId = config.Facebook.ClientId;
                options.ClientSecret = config.Facebook.ClientSecret;
            })
            .AddTwitter(options =>
            {
                options.ConsumerKey = config.Twitter.ClientId;
                options.ConsumerSecret = config.Twitter.ClientSecret;
            });

        return builder;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication").Get<AuthenticationConfiguration>();
        if (authConfig == null)
        {
            throw new InvalidOperationException("Authentication configuration is missing");
        }

        services.AddSingleton(authConfig);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authConfig.JwtIssuer,
                ValidAudience = authConfig.JwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.JwtSecret))
            };
        })
        .AddOAuthProviders(authConfig);

        return services;
    }
} 