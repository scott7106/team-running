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
    /// Gets the theme information for a subdomain.
    /// </summary>
    /// <returns>A SubdomainDto object with theme information for the team.</returns>
    Task<SubdomainDto> GetThemeInfoByDomainAsync(string subdomain);

    /// <summary>
    /// Gets theme information for multiple teams by their IDs.
    /// </summary>
    /// <param name="teamIds">List of team IDs to get theme information for</param>
    /// <returns>A collection of SubdomainDto objects with theme information for the teams.</returns>
    Task<IEnumerable<SubdomainDto>> GetThemeInfoByIdsAsync(IEnumerable<Guid> teamIds);
} 