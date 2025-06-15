using TeamStride.Application.Teams.Dtos;

namespace TeamStride.Application.Teams.Services;

/// <summary>
/// Service interface for public site registration functionality.
/// Handles public team registration and subdomain validation without authentication requirements.
/// Used by PublicTeamsController for unauthenticated users registering teams.
/// </summary>
public interface ISiteRegistrationService
{
    /// <summary>
    /// Checks if a subdomain is available for team registration.
    /// This is a public operation that doesn't require authentication.
    /// </summary>
    /// <param name="subdomain">Subdomain to check availability for</param>
    /// <returns>True if subdomain is available, false otherwise</returns>
    Task<bool> IsSubdomainAvailableAsync(string subdomain);

    /// <summary>
    /// Creates a new team with a new user as owner via public registration.
    /// This is a public operation that doesn't require authentication.
    /// Creates both the user and team in a single transaction.
    /// </summary>
    /// <param name="dto">Team and owner creation data</param>
    /// <returns>Result containing team details and redirect information</returns>
    Task<PublicTeamCreationResultDto> CreateTeamWithNewOwnerAsync(CreateTeamWithNewOwnerDto dto);
} 