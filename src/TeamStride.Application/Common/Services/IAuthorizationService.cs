using TeamStride.Domain.Entities;

namespace TeamStride.Application.Common.Services;

/// <summary>
/// Centralized authorization service that provides reusable authorization logic
/// for the simplified 3-tier authorization model across all application services.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Requires the current user to be a global admin.
    /// Throws UnauthorizedAccessException if the user is not authenticated or not a global admin.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated or not a global admin</exception>
    Task RequireGlobalAdminAsync();

    /// <summary>
    /// Requires the current user to have access to the specified team with the minimum required role.
    /// Global admins bypass team access restrictions.
    /// </summary>
    /// <param name="teamId">The team ID to check access for</param>
    /// <param name="minimumRole">The minimum role required (default: TeamMember)</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks sufficient access</exception>
    Task RequireTeamAccessAsync(Guid teamId, TeamRole minimumRole = TeamRole.TeamMember);

    /// <summary>
    /// Requires the current user to be the owner of the specified team.
    /// Global admins bypass ownership restrictions.
    /// </summary>
    /// <param name="teamId">The team ID to check ownership for</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not the team owner</exception>
    Task RequireTeamOwnershipAsync(Guid teamId);

    /// <summary>
    /// Requires the current user to have admin privileges (TeamOwner or TeamAdmin) for the specified team.
    /// Global admins bypass team admin restrictions.
    /// </summary>
    /// <param name="teamId">The team ID to check admin access for</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks admin privileges</exception>
    Task RequireTeamAdminAsync(Guid teamId);

    /// <summary>
    /// Checks if the current user can access the specified team with the minimum required role.
    /// Global admins can access any team.
    /// </summary>
    /// <param name="teamId">The team ID to check access for</param>
    /// <param name="minimumRole">The minimum role required (default: TeamMember)</param>
    /// <returns>True if user has access, false otherwise</returns>
    Task<bool> CanAccessTeamAsync(Guid teamId, TeamRole minimumRole = TeamRole.TeamMember);

    /// <summary>
    /// Checks if the current user is the owner of the specified team.
    /// Global admins are considered owners of all teams.
    /// </summary>
    /// <param name="teamId">The team ID to check ownership for</param>
    /// <returns>True if user is the team owner or global admin, false otherwise</returns>
    Task<bool> IsTeamOwnerAsync(Guid teamId);

    /// <summary>
    /// Checks if the current user has admin privileges (TeamOwner or TeamAdmin) for the specified team.
    /// Global admins have admin privileges for all teams.
    /// </summary>
    /// <param name="teamId">The team ID to check admin access for</param>
    /// <returns>True if user has admin privileges, false otherwise</returns>
    Task<bool> IsTeamAdminAsync(Guid teamId);

    /// <summary>
    /// Validates that a resource belongs to a team that the current user can access.
    /// Used for ensuring users can only access resources from their authorized teams.
    /// </summary>
    /// <typeparam name="T">The resource type that implements ITeamResource</typeparam>
    /// <param name="resource">The resource to validate</param>
    /// <param name="minimumRole">The minimum role required to access the resource</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when user cannot access the resource's team</exception>
    Task RequireResourceAccessAsync<T>(T resource, TeamRole minimumRole = TeamRole.TeamMember) where T : ITeamResource;

    /// <summary>
    /// Checks if the current user can access a resource based on its team association.
    /// </summary>
    /// <typeparam name="T">The resource type that implements ITeamResource</typeparam>
    /// <param name="resource">The resource to check</param>
    /// <param name="minimumRole">The minimum role required to access the resource</param>
    /// <returns>True if user can access the resource, false otherwise</returns>
    Task<bool> CanAccessResourceAsync<T>(T resource, TeamRole minimumRole = TeamRole.TeamMember) where T : ITeamResource;
}

/// <summary>
/// Interface for entities that belong to a team and can be used with resource-based authorization.
/// </summary>
public interface ITeamResource
{
    /// <summary>
    /// The ID of the team that owns this resource.
    /// </summary>
    Guid TeamId { get; }
} 