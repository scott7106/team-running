using Microsoft.Extensions.Logging;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private readonly ITenantService _tenantService;

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger,
        ITenantService tenantService)
    {
        _next = next;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Value;

        try
        {
            // Skip tenant resolution for the main marketing site and API endpoints
            if (!host.Contains(".") || host.StartsWith("api."))
            {
                _tenantService.ClearCurrentTenant();
                await _next(context);
                return;
            }

            // Extract subdomain
            var subdomain = host.Split('.')[0];
            _logger.LogInformation("Resolving tenant for subdomain: {Subdomain}", subdomain);

            // Set the current tenant
            _tenantService.SetCurrentTenant(subdomain);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tenant middleware for host: {Host}", host);
            throw;
        }
        finally
        {
            _tenantService.ClearCurrentTenant();
        }
    }
} 