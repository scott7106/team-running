using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;

namespace TeamStride.Domain.Interfaces;

/// <summary>
/// Domain service interface for core team management operations.
/// Provides domain-level functionality for team creation, subdomain management, and team retrieval.
/// Does not handle authentication, authorization, user creation, or transaction management - those are handled by application services.
/// </summary>
public interface ITeamManager
{
    /// <summary>
    /// Checks if a subdomain is available for team registration.
    /// </summary>
    /// <param name="subdomain">Subdomain to check</param>
    /// <param name="excludeTeamId">Optional team ID to exclude from check (for updates)</param>
    /// <returns>True if subdomain is available, false otherwise</returns>
    Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTeamId = null);

    /// <summary>
    /// Retrieves a team by its subdomain without any authorization checks.
    /// Used for public team lookup and domain resolution.
    /// </summary>
    /// <param name="subdomain">Team subdomain</param>
    /// <returns>Team entity if found</returns>
    /// <exception cref="InvalidOperationException">Thrown when team is not found</exception>
    Task<Team> GetTeamBySubdomainAsync(string subdomain);

    /// <summary>
    /// Creates a team with the specified owner. Does not create user accounts or manage transactions.
    /// The owner must already exist in the system.
    /// This method should be called within an existing transaction scope.
    /// </summary>
    /// <param name="request">Team creation request with owner information</param>
    /// <returns>Created team entity</returns>
    /// <exception cref="InvalidOperationException">Thrown when subdomain is taken or owner doesn't exist</exception>
    Task<Team> CreateTeamAsync(CreateTeamRequest request);

    /// <summary>
    /// Validates subdomain format and characters.
    /// </summary>
    /// <param name="subdomain">Subdomain to validate</param>
    /// <returns>True if valid format, false otherwise</returns>
    bool IsValidSubdomainFormat(string subdomain);

    /// <summary>
    /// Normalizes subdomain to lowercase and standard format.
    /// </summary>
    /// <param name="subdomain">Raw subdomain input</param>
    /// <returns>Normalized subdomain</returns>
    string NormalizeSubdomain(string subdomain);
}

/// <summary>
/// Request model for team creation through TeamManager.
/// Contains only the essential data needed to create a team, without user creation concerns.
/// </summary>
public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public TeamTier Tier { get; set; }
    public TeamStatus Status { get; set; } = TeamStatus.Active;
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public DateTime? ExpiresOn { get; set; }
    public string? LogoUrl { get; set; }
}

 