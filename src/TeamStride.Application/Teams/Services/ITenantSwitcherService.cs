using TeamStride.Application.Teams.Dtos;

namespace TeamStride.Application.Teams.Services;

public interface ITenantSwitcherService
{
    /// <summary>
    /// Gets all tenants (teams) that the current user has access to for tenant switching.
    /// </summary>
    /// <returns>A collection of TenantDto objects representing teams the user can switch to.</returns>
    Task<IEnumerable<TenantDto>> GetUserTenantsAsync();

    /// <summary>
    /// Gets all active tenants (teams) for public access - used by middleware for subdomain resolution.
    /// </summary>
    /// <returns>A collection of SubdomainDto objects representing all active team subdomains.</returns>
    Task<IEnumerable<SubdomainDto>> GetAllTenantsAsync();
} 