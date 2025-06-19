using Microsoft.Extensions.DependencyInjection;
using TeamStride.Domain.Interfaces;
using System.Text.Json;

namespace TeamStride.Api.Middleware;

/// <summary>
/// Middleware to set team context for API endpoints from JWT claims when no subdomain is present.
/// Only applies to endpoints starting with "api/teams".
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
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        // Only apply this middleware to endpoints starting with "api/teams"
        if (!path?.StartsWith("/api/teams") == true)
        {
            await _next(context);
            return;
        }

        var host = context.Request.Host.Value;
        _logger.LogDebug("Processing API team context for path: {Path}, host: {Host}", path, host);

        var teamService = context.RequestServices.GetRequiredService<ICurrentTeamService>();
        var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();

        try
        {
            _logger.LogDebug("Team context check - IsTeamSet: {IsTeamSet}, IsAuthenticated: {IsAuthenticated}, CurrentSubdomain: {Subdomain}", 
                teamService.IsTeamSet, currentUserService.IsAuthenticated, teamService.GetSubdomain ?? "null");

            // Only process if team context is not already set
            if (teamService.IsTeamSet)
            {
                _logger.LogDebug("Team context already set to {TeamId}, skipping JWT claims processing", teamService.TeamId);
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

            _logger.LogDebug("Attempting to set team context from JWT claims for user {UserId}", currentUserService.UserId);

            // For API endpoints, we may not have subdomain context from the host
            // Try to determine subdomain from the request or use a fallback strategy
            TrySetSubdomainContextForApiRequest(context, teamService);

            // Try to set team context from JWT claims if user is authenticated
            var teamSetFromJwt = teamService.SetTeamFromJwtClaims();
            
            _logger.LogDebug("SetTeamFromJwtClaims returned: {Result}", teamSetFromJwt);
            
            if (teamSetFromJwt)
            {
                if (teamService.IsTeamSet)
                {
                    _logger.LogInformation("Team context set from JWT claims for user {UserId} to team {TeamId}", 
                        currentUserService.UserId, teamService.TeamId);
                }
                else
                {
                    _logger.LogWarning("SetTeamFromJwtClaims returned true but team is not set for user {UserId}", 
                        currentUserService.UserId);
                }
            }
            else
            {
                _logger.LogDebug("Could not set team context from JWT claims for user {UserId}", 
                    currentUserService.UserId);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing API team context middleware for path: {Path}", context.Request.Path);
            await HandleMiddlewareError(context, "Internal server error during API team context resolution");
        }
    }

    private void TrySetSubdomainContextForApiRequest(HttpContext context, ICurrentTeamService teamService)
    {
        try
        {
            var host = context.Request.Host.Value;
            _logger.LogDebug("Attempting to determine subdomain context for API request. Host: {Host}", host);

            // Check if we already have subdomain context (set by TeamMiddleware)
            if (!string.IsNullOrEmpty(teamService.GetSubdomain))
            {
                _logger.LogDebug("Subdomain already set: {Subdomain}", teamService.GetSubdomain);
                return;
            }

            // Try to extract subdomain from host
            var hostParts = host.Split('.');
            if (hostParts.Length > 2 && !host.StartsWith("api.") && !host.StartsWith("www."))
            {
                var subdomain = hostParts[0];
                _logger.LogDebug("Extracted subdomain from host: {Subdomain}", subdomain);
                teamService.SetTeamSubdomain(subdomain);
                return;
            }

            // Check for subdomain in headers (for cases where client sends it)
            var subdomainHeader = context.Request.Headers["X-Subdomain"].FirstOrDefault();
            if (!string.IsNullOrEmpty(subdomainHeader))
            {
                _logger.LogDebug("Using subdomain from X-Subdomain header: {Subdomain}", subdomainHeader);
                teamService.SetTeamSubdomain(subdomainHeader);
                return;
            }

            // Development fallback - check query parameter
            #if DEBUG
            if (host.StartsWith("localhost") && context.Request.Query.ContainsKey("subdomain"))
            {
                var querySubdomain = context.Request.Query["subdomain"].ToString();
                _logger.LogInformation("Using subdomain from query parameter for development: {Subdomain}", querySubdomain);
                teamService.SetTeamSubdomain(querySubdomain);
                return;
            }
            #endif

            _logger.LogWarning("Could not determine subdomain context for API request. Host: {Host}", host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to set subdomain context for API request");
        }
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