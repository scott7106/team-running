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
    /// <param name="excludeTeamId">Optional team ID to exclude from the check (for editing existing teams)</param>
    /// <returns>True if subdomain is available, false otherwise</returns>
    Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTeamId = null);

    /// <summary>
    /// Creates a new team with a new user as owner via public registration.
    /// This is a public operation that doesn't require authentication.
    /// Creates both the user and team in a single transaction.
    /// </summary>
    /// <param name="dto">Team and owner creation data</param>
    /// <returns>Result containing team details and redirect information</returns>
    Task<PublicTeamCreationResultDto> CreateTeamWithNewOwnerAsync(CreateTeamWithNewOwnerDto dto);

    /// <summary>
    /// Creates a new team with an existing authenticated user as owner via public registration.
    /// This requires authentication and uses the current user as the team owner.
    /// </summary>
    /// <param name="dto">Team creation data</param>
    /// <param name="currentUserId">ID of the authenticated user who will become the team owner</param>
    /// <returns>Result containing team details and redirect information</returns>
    Task<PublicTeamCreationResultDto> CreateTeamWithExistingOwnerAsync(CreateTeamWithExistingOwnerDto dto, Guid currentUserId);
} 