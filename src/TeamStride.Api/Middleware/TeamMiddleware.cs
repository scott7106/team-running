using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TeamStride.Domain.Interfaces;

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
        var teamService = context.RequestServices.GetRequiredService<ITeamService>();

        try
        {
            // Skip team resolution for the main marketing site and API endpoints
            if (!host.Contains(".") || host.StartsWith("api."))
            {
                teamService.ClearCurrentTeam();
                await _next(context);
                return;
            }

            // Extract subdomain
            var subdomain = host.Split('.')[0];
            _logger.LogInformation("Resolving team for subdomain: {Subdomain}", subdomain);

            // Set the current team
            teamService.SetCurrentTeam(subdomain);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing team middleware for host: {Host}", host);
            throw;
        }
        finally
        {
            teamService.ClearCurrentTeam();
        }
    }
} 