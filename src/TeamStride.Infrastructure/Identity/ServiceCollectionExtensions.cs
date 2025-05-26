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
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Authentication.Services;
using TeamStride.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using TeamStride.Infrastructure.Services;
using TeamStride.Application.Authentication.Services;

namespace TeamStride.Infrastructure.Identity;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamStrideIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication").Get<AuthenticationConfiguration>();
        ArgumentNullException.ThrowIfNull(authConfig);
        
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddSingleton(authConfig);
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ITeamStrideAuthenticationService, AuthenticationService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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
        ArgumentNullException.ThrowIfNull(authConfig);

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