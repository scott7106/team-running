using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using TeamStride.Api.Middleware;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TeamStride.Infrastructure.Email;
using TeamStride.Infrastructure.Identity;
using Microsoft.AspNetCore.Routing;
using TeamStride.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Configuration;
using TeamStride.Infrastructure.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Services;

namespace TeamStride.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add services to the container
        builder.Services.AddControllers(options =>
        {
            // Add kebab-case route convention
            options.Conventions.Add(new RouteTokenTransformerConvention(
                new SlugifyParameterTransformer()));
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TeamStride API",
                Version = "v1",
                Description = "API for TeamStride running team management platform"
            });

            // Enable XML comments in Swagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });

        // Add HttpContextAccessor
        builder.Services.AddHttpContextAccessor();

        // Register application services
        builder.Services.AddScoped<ICurrentTeamService, CurrentTeamService>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IAuthorizationService, TeamStride.Infrastructure.Services.AuthorizationService>();
        builder.Services
            .AddInfrastructureServices()
            .AddApplicationServices();

        // Register services
        builder.Services.AddScoped<ITeamRegistrationService, TeamStride.Infrastructure.Services.TeamRegistrationService>();

        // Configure DbContext
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Services
            .AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }))
            .AddTeamStrideIdentity(builder.Configuration)
            .AddEmailServices(builder.Configuration)
            .AddGlobalAdminSeeder(builder.Configuration)
            .AddApplicationRoleSeeder()
            .AddDevelopmentTestDataSeeder(builder.Configuration);

        // Configure JWT authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var config = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfiguration>() ?? 
                throw new InvalidOperationException("Authentication configuration is missing");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config.JwtIssuer,
                ValidAudience = config.JwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(config.JwtSecret))
            };
        });

        // Configure rate limiting
        builder.Services.Configure<TeamStride.Infrastructure.Configuration.RateLimitingOptions>(
            builder.Configuration.GetSection(TeamStride.Infrastructure.Configuration.RateLimitingOptions.SectionName));

        // Configure app settings
        builder.Services.Configure<TeamStride.Infrastructure.Configuration.AppConfiguration>(
            builder.Configuration.GetSection(TeamStride.Infrastructure.Configuration.AppConfiguration.SectionName));

        // Configure CORS to allow requests from team subdomains
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowTeamSubdomains", policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrEmpty(origin))
                            return false;

                        // Allow localhost:3000 and all team subdomains like eagles.localhost:3000
                        return origin == "http://localhost:3000" || 
                               origin == "https://localhost:3000" ||
                               (origin.StartsWith("http://") && origin.EndsWith(".localhost:3000")) ||
                               (origin.StartsWith("https://") && origin.EndsWith(".localhost:3000")) ||
                               // For production, allow teamstride.net subdomains
                               (origin.StartsWith("https://") && origin.EndsWith(".teamstride.net"));
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeamStride API V1");
                c.RoutePrefix = "swagger";
            });
        }
        else
        {
            // In non-development environments, serve the Next.js static files
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        app.UseHttpsRedirection();

        // Use CORS
        app.UseCors("AllowTeamSubdomains");

        // Add team resolution before authentication
        app.UseTeamResolution();

        // Add exception handling middleware
        app.UseExceptionHandling();

        // Add authentication and authorization middleware
        app.UseAuthentication();
        
        // Add API team context middleware after authentication but before authorization
        app.UseApiTeamContext();
        
        app.UseAuthorization();

        // Add middleware
        app.UseMiddleware<RateLimitingMiddleware>();

        app.MapControllers();

        // In non-development environments, add fallback routing for SPA
        if (!app.Environment.IsDevelopment())
        {
            app.MapFallbackToFile("index.html");
        }

        try
        {
            // Apply migrations only for relational databases (not in-memory)
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (context.Database.IsRelational())
                {
                    context.Database.Migrate();
                }
            }

            Log.Information("Starting TeamStride API");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
