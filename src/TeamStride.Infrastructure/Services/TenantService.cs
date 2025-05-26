using Microsoft.Extensions.Logging;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ILogger<TenantService> _logger;
    private Guid? _currentTenantId;
    private string? _currentSubdomain;

    public TenantService(ILogger<TenantService> logger)
    {
        _logger = logger;
    }

    public Guid CurrentTenantId
    {
        get
        {
            if (!_currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant is not set");
            }
            return _currentTenantId.Value;
        }
    }

    public string? CurrentTenantSubdomain => _currentSubdomain;

    public void SetCurrentTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
        _logger.LogInformation("Current tenant set to {TenantId}", tenantId);
    }

    public void SetCurrentTenant(string subdomain)
    {
        _currentSubdomain = subdomain;
        _logger.LogInformation("Current tenant subdomain set to {Subdomain}", subdomain);
        // Note: The actual tenant ID should be resolved from the database
        // This will be implemented when we add the tenant resolution middleware
    }

    public void ClearCurrentTenant()
    {
        _currentTenantId = null;
        _currentSubdomain = null;
        _logger.LogInformation("Current tenant cleared");
    }
} 