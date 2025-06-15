using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamStride.Infrastructure.Configuration;

namespace TeamStride.Infrastructure.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly TeamStride.Infrastructure.Configuration.RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _ipLimits = new();
    private readonly ConcurrentDictionary<string, RateLimitInfo> _deviceLimits = new();
    private readonly ConcurrentDictionary<string, RateLimitInfo> _emailLimits = new();
    private readonly ConcurrentDictionary<string, RateLimitInfo> _teamLimits = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<TeamStride.Infrastructure.Configuration.RateLimitingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var routeData = context.GetRouteData();
        if (routeData == null)
        {
            await _next(context);
            return;
        }

        // Check IP-based rate limit
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ipAddress) && !CheckRateLimit(_ipLimits, ipAddress, _options.MaxRequestsPerIp, _options.WindowMinutes))
        {
            await ReturnRateLimitExceeded(context, "IP");
            return;
        }

        // Check device-based rate limit
        var deviceId = context.Request.Headers["X-Device-ID"].ToString();
        if (!string.IsNullOrEmpty(deviceId) && !CheckRateLimit(_deviceLimits, deviceId, _options.MaxRequestsPerDevice, _options.WindowMinutes))
        {
            await ReturnRateLimitExceeded(context, "Device");
            return;
        }

        // Check email-based rate limit for registration endpoints
        if (IsRegistrationEndpoint(routeData))
        {
            if (context.Request.HasFormContentType)
            {
                var email = context.Request.Form["email"].ToString();
                if (!string.IsNullOrEmpty(email) && !CheckRateLimit(_emailLimits, email, _options.MaxRequestsPerEmail, _options.WindowMinutes))
                {
                    await ReturnRateLimitExceeded(context, "Email");
                    return;
                }
            }
        }

        // Check team-based rate limit for team-specific endpoints
        if (IsTeamEndpoint(routeData))
        {
            var teamId = routeData.Values["teamId"]?.ToString();
            if (!string.IsNullOrEmpty(teamId) && !CheckRateLimit(_teamLimits, teamId, _options.MaxRequestsPerTeam, _options.WindowMinutes))
            {
                await ReturnRateLimitExceeded(context, "Team");
                return;
            }
        }

        await _next(context);
    }

    private bool IsRegistrationEndpoint(RouteData routeData)
    {
        var controller = routeData.Values["controller"]?.ToString()?.ToLower();
        var action = routeData.Values["action"]?.ToString()?.ToLower();
        
        // Check for team registration endpoints
        if (controller == "teamregistration")
        {
            return true;
        }
        
        // Check for account registration endpoints
        if (controller == "account" && (action == "register" || action == "confirmemail"))
        {
            return true;
        }
        
        return false;
    }

    private bool IsTeamEndpoint(RouteData routeData)
    {
        var controller = routeData.Values["controller"]?.ToString()?.ToLower();
        return controller == "team" || 
               controller == "athlete" || 
               controller == "practice" ||
               controller == "teamregistration";
    }

    private bool CheckRateLimit(ConcurrentDictionary<string, RateLimitInfo> limits, string key, int maxRequests, int windowMinutes)
    {
        var now = DateTime.UtcNow;
        var limit = limits.GetOrAdd(key, _ => new RateLimitInfo { LastReset = now });

        if (now - limit.LastReset > TimeSpan.FromMinutes(windowMinutes))
        {
            limit.Count = 0;
            limit.LastReset = now;
        }

        if (limit.Count >= maxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for {Key}. Count: {Count}, Max: {Max}, Window: {Window} minutes",
                key, limit.Count, maxRequests, windowMinutes);
            return false;
        }

        limit.Count++;
        return true;
    }

    private async Task ReturnRateLimitExceeded(HttpContext context, string limitType)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = (_options.WindowMinutes * 60).ToString();
        await context.Response.WriteAsync($"Rate limit exceeded. Please try again in {_options.WindowMinutes} minutes.");
    }
}

public class RateLimitInfo
{
    public int Count { get; set; }
    public DateTime LastReset { get; set; }
} 