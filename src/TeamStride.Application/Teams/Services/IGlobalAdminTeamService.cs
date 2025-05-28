using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Services;

/// <summary>
/// Service interface for global admin team management operations.
/// These operations bypass normal team access restrictions and are only available to global admins.
/// </summary>
public interface IGlobalAdminTeamService
{
    /// <summary>
    /// Gets a paginated list of all teams in the system with search and filtering capabilities.
    /// Global query filters are disabled for this operation.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by team name, subdomain, or owner email</param>
    /// <param name="status">Filter by team status</param>
    /// <param name="tier">Filter by team tier</param>
    /// <param name="expiresOnFrom">Filter teams expiring after this date</param>
    /// <param name="expiresOnTo">Filter teams expiring before this date</param>
    /// <returns>Paginated list of teams</returns>
    Task<PaginatedList<GlobalAdminTeamDto>> GetTeamsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null,
        TeamStatus? status = null,
        TeamTier? tier = null,
        DateTime? expiresOnFrom = null,
        DateTime? expiresOnTo = null);

    /// <summary>
    /// Gets a paginated list of deleted teams that can be recovered.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by team name, subdomain, or owner email</param>
    /// <returns>Paginated list of deleted teams</returns>
    Task<PaginatedList<DeletedTeamDto>> GetDeletedTeamsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null);

    /// <summary>
    /// Gets a team by ID (bypasses team access restrictions).
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Team details</returns>
    Task<GlobalAdminTeamDto> GetTeamByIdAsync(Guid teamId);

    /// <summary>
    /// Creates a new team with a new user as the owner.
    /// Creates both the user account and the team, then establishes the ownership relationship.
    /// </summary>
    /// <param name="dto">Team and owner creation data</param>
    /// <returns>Created team details</returns>
    Task<GlobalAdminTeamDto> CreateTeamWithNewOwnerAsync(CreateTeamWithNewOwnerDto dto);

    /// <summary>
    /// Creates a new team with an existing user as the owner.
    /// The user must exist and cannot already be the owner of another team.
    /// </summary>
    /// <param name="dto">Team creation data with existing owner</param>
    /// <returns>Created team details</returns>
    Task<GlobalAdminTeamDto> CreateTeamWithExistingOwnerAsync(CreateTeamWithExistingOwnerDto dto);

    /// <summary>
    /// Updates a team's properties (all non-audit properties).
    /// This operation bypasses normal team access restrictions.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated team details</returns>
    Task<GlobalAdminTeamDto> UpdateTeamAsync(Guid teamId, GlobalAdminUpdateTeamDto dto);

    /// <summary>
    /// Soft deletes a team and all its associated data.
    /// The team can be recovered using RecoverTeamAsync.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Task</returns>
    Task DeleteTeamAsync(Guid teamId);

    /// <summary>
    /// Permanently removes a team and all its associated data.
    /// This operation cannot be undone.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Task</returns>
    Task PermanentlyDeleteTeamAsync(Guid teamId);

    /// <summary>
    /// Recovers a soft-deleted team and restores its active status.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Recovered team details</returns>
    Task<GlobalAdminTeamDto> RecoverTeamAsync(Guid teamId);

    /// <summary>
    /// Initiates an immediate ownership transfer (bypasses normal transfer process).
    /// Updates both the Team.OwnerId and the UserTeam roles in a single transaction.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Transfer data</param>
    /// <returns>Updated team details</returns>
    Task<GlobalAdminTeamDto> TransferOwnershipAsync(Guid teamId, GlobalAdminTransferOwnershipDto dto);

    /// <summary>
    /// Validates that a subdomain is available for use.
    /// </summary>
    /// <param name="subdomain">Subdomain to check</param>
    /// <param name="excludeTeamId">Team ID to exclude from the check (for updates)</param>
    /// <returns>True if available, false if taken</returns>
    Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTeamId = null);
} 