using Microsoft.Extensions.DependencyInjection;
using TeamStride.Domain.Interfaces;
using System.Text.Json;

namespace TeamStride.Api.Middleware;

/// <summary>
/// Middleware to set team context for API endpoints from JWT claims when no subdomain is present
/// </summary>
public class ApiTeamContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiTeamContextMiddleware> _logger;

    public ApiTeamContextMiddleware(
        RequestDelegate next,
        ILogger<ApiTeamContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var teamService = context.RequestServices.GetRequiredService<ICurrentTeamService>();
        var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();

        try
        {
            // Only process API endpoints that don't already have team context set
            var path = context.Request.Path.Value?.ToLowerInvariant();
            var isApiEndpoint = path?.StartsWith("/api/") == true;
            
            if (!isApiEndpoint || teamService.IsTeamSet)
            {
                await _next(context);
                return;
            }

            // Skip team context for authentication endpoints and global admin endpoints
            if (IsAuthenticationEndpoint(path) || IsGlobalAdminEndpoint(path))
            {
                _logger.LogDebug("Skipping team context for endpoint: {Path}", path);
                await _next(context);
                return;
            }

            // Only set team context for authenticated users
            if (!currentUserService.IsAuthenticated)
            {
                _logger.LogDebug("User not authenticated, skipping team context for API endpoint: {Path}", path);
                await _next(context);
                return;
            }

            // Try to set team context from JWT claims if user is authenticated
            if (currentUserService.IsAuthenticated)
            {
                var teamSetFromJwt = teamService.SetTeamFromJwtClaims();
                if (teamSetFromJwt)
                {
                    _logger.LogInformation("Team context set from JWT claims for user {UserId} to team {TeamId}", 
                        currentUserService.UserId, teamService.TeamId);
                }
                else
                {
                    _logger.LogDebug("Could not set team context from JWT claims for user {UserId}", 
                        currentUserService.UserId);
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing API team context middleware for path: {Path}", context.Request.Path);
            await HandleMiddlewareError(context, "Internal server error during API team context resolution");
        }
    }

    private static bool IsAuthenticationEndpoint(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        
        return path.Contains("/auth/") || 
               path.Contains("/login") || 
               path.Contains("/register") || 
               path.Contains("/token") ||
               path.Contains("/refresh");
    }

    private static bool IsGlobalAdminEndpoint(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        
        return path.Contains("/admin/") ||
               path.StartsWith("/api/team-management") && !path.Contains("/subdomain/");
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