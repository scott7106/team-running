using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TeamStride.Domain.Interfaces;
using System.Text.Json;

namespace TeamStride.Api.Middleware;

public class TeamMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TeamMiddleware> _logger;

    public TeamMiddleware(
        RequestDelegate next,
        ILogger<TeamMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Value;
        var teamService = context.RequestServices.GetRequiredService<ICurrentTeamService>();
        var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();

        try
        {
            // Clear any existing team context
            teamService.ClearTeam();

            // Skip team resolution for the main marketing site and API endpoints without subdomains
            var hostParts = host.Split('.');
            var isMainSite = hostParts.Length <= 2 || host.StartsWith("api.") || host.StartsWith("www.");
            
            if (isMainSite)
            {
                _logger.LogDebug("Skipping team resolution for host: {Host}", host);
                await _next(context);
                return;
            }

            // Extract subdomain (should be the first part for team subdomains like team.teamstride.com)
            var subdomain = hostParts[0];
            _logger.LogInformation("Processing team resolution for subdomain: {Subdomain}", subdomain);

            // Validate subdomain format
            if (string.IsNullOrWhiteSpace(subdomain) || subdomain.Length < 3)
            {
                _logger.LogWarning("Invalid subdomain format: {Subdomain}", subdomain);
                await HandleInvalidTeamContext(context, "Invalid subdomain format");
                return;
            }

            // Try to resolve team from subdomain first
            var teamResolvedFromSubdomain = await teamService.SetTeamFromSubdomainAsync(subdomain);
            
            if (!teamResolvedFromSubdomain)
            {
                _logger.LogWarning("Team not found for subdomain: {Subdomain}", subdomain);
                await HandleInvalidTeamContext(context, "Team not found");
                return;
            }

            // If user is authenticated, validate they have access to this team
            if (currentUserService.IsAuthenticated)
            {
                var teamId = teamService.TeamId;
                
                // Global admins can access any team
                if (!currentUserService.IsGlobalAdmin && !currentUserService.CanAccessTeam(teamId))
                {
                    _logger.LogWarning("User {UserId} attempted to access team {TeamId} via subdomain {Subdomain} without permission", 
                        currentUserService.UserId, teamId, subdomain);
                    await HandleUnauthorizedTeamAccess(context, "Access denied to this team");
                    return;
                }

                _logger.LogInformation("Team context successfully set for authenticated user {UserId} to team {TeamId} via subdomain {Subdomain}", 
                    currentUserService.UserId, teamId, subdomain);
            }
            else
            {
                _logger.LogInformation("Team context set for unauthenticated user to team {TeamId} via subdomain {Subdomain}", 
                    teamService.TeamId, subdomain);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing team middleware for host: {Host}", host);
            await HandleMiddlewareError(context, "Internal server error during team resolution");
        }
        finally
        {
            // Clear team context after request processing
            teamService.ClearTeam();
        }
    }

    private async Task HandleInvalidTeamContext(HttpContext context, string message)
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Team Not Found",
            message = message,
            statusCode = 404
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleUnauthorizedTeamAccess(HttpContext context, string message)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Access Denied",
            message = message,
            statusCode = 403
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleMiddlewareError(HttpContext context, string message)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Internal Server Error",
            message = message,
            statusCode = 500
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
} 