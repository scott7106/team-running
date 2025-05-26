using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Value;
        var tenantService = context.RequestServices.GetRequiredService<ITenantService>();

        try
        {
            // Skip tenant resolution for the main marketing site and API endpoints
            if (!host.Contains(".") || host.StartsWith("api."))
            {
                tenantService.ClearCurrentTenant();
                await _next(context);
                return;
            }

            // Extract subdomain
            var subdomain = host.Split('.')[0];
            _logger.LogInformation("Resolving tenant for subdomain: {Subdomain}", subdomain);

            // Set the current tenant
            tenantService.SetCurrentTenant(subdomain);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tenant middleware for host: {Host}", host);
            throw;
        }
        finally
        {
            tenantService.ClearCurrentTenant();
        }
    }
} 