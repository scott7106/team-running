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

namespace TeamStride.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/teamstride-.txt", rollingInterval: RollingInterval.Day)
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
        builder.Services.AddScoped<TeamStride.Application.Common.Services.IAuthorizationService, TeamStride.Infrastructure.Services.AuthorizationService>();
        builder.Services
            .AddInfrastructureServices()
            .AddApplicationServices();

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
            .AddEmailService(builder.Configuration, isDevelopment)
            .AddGlobalAdminSeeder(builder.Configuration)
            .AddApplicationRoleSeeder();

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

        // Add team resolution before authentication
        app.UseTeamResolution();

        // Add exception handling middleware
        app.UseExceptionHandling();

        // Add authentication and authorization middleware
        app.UseAuthentication();
        
        // Add API team context middleware after authentication but before authorization
        app.UseApiTeamContext();
        
        app.UseAuthorization();

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
