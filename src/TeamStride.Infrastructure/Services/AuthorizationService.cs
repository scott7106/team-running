using Microsoft.Extensions.Logging;
using TeamStride.Application.Common.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Services;

/// <summary>
/// Centralized authorization service implementation that provides reusable authorization logic
/// for the simplified 3-tier authorization model across all application services.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationService> _logger;
    private readonly ApplicationDbContext _context;

    public AuthorizationService(
        ICurrentUserService currentUserService,
        ILogger<AuthorizationService> logger,
        ApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _context = context;
    }

    public Task RequireGlobalAdminAsync()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning("Unauthorized access attempt - user not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        if (!_currentUserService.IsGlobalAdmin)
        {
            _logger.LogWarning("Access denied - user {UserId} is not a global admin", _currentUserService.UserId);
            throw new UnauthorizedAccessException("Global admin privileges required");
        }

        _logger.LogDebug("Global admin access granted for user {UserId}", _currentUserService.UserId);
        return Task.CompletedTask;
    }

    public Task RequireTeamAccessAsync(Guid teamId, TeamRole minimumRole = TeamRole.TeamMember)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning("Unauthorized access attempt to team {TeamId} - user not authenticated", teamId);
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Global admins bypass all team access restrictions
        if (_currentUserService.IsGlobalAdmin)
        {
            _logger.LogDebug("Team access granted to global admin {UserId} for team {TeamId}", 
                _currentUserService.UserId, teamId);
            return Task.CompletedTask;
        }

        // Check if user can access the team
        if (!_currentUserService.CanAccessTeam(teamId))
        {
            _logger.LogWarning("Access denied - user {UserId} cannot access team {TeamId}", 
                _currentUserService.UserId, teamId);
            throw new UnauthorizedAccessException($"Access denied to team {teamId}");
        }

        // Check if user has sufficient role
        if (!_currentUserService.HasMinimumTeamRole(minimumRole))
        {
            _logger.LogWarning("Access denied - user {UserId} has role {UserRole} but {MinimumRole} required for team {TeamId}", 
                _currentUserService.UserId, _currentUserService.HasMinimumTeamRole(minimumRole), minimumRole, teamId);
            throw new UnauthorizedAccessException($"Minimum role {minimumRole} required");
        }

        _logger.LogDebug("Team access granted for user {UserId} with role {UserRole} to team {TeamId}", 
            _currentUserService.UserId, _currentUserService.HasMinimumTeamRole(minimumRole), teamId);
        return Task.CompletedTask;
    }

    public Task RequireTeamOwnershipAsync(Guid teamId)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning("Unauthorized ownership access attempt to team {TeamId} - user not authenticated", teamId);
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Global admins bypass ownership restrictions
        if (_currentUserService.IsGlobalAdmin)
        {
            _logger.LogDebug("Team ownership granted to global admin {UserId} for team {TeamId}", 
                _currentUserService.UserId, teamId);
            return Task.CompletedTask;
        }

        // Check if user can access the team and is the owner
        if (!_currentUserService.CanAccessTeam(teamId) || !_currentUserService.HasMinimumTeamRole(TeamRole.TeamOwner))
        {
            _logger.LogWarning("Ownership denied - user {UserId} is not the owner of team {TeamId}", 
                _currentUserService.UserId, teamId);
            throw new UnauthorizedAccessException($"Team ownership required for team {teamId}");
        }

        _logger.LogDebug("Team ownership verified for user {UserId} on team {TeamId}", 
            _currentUserService.UserId, teamId);
        return Task.CompletedTask;
    }

    public Task RequireTeamAdminAsync(Guid teamId)
    {
        return RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);
    }

    public Task<bool> CanAccessTeamAsync(Guid teamId, TeamRole minimumRole = TeamRole.TeamMember)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Task.FromResult(false);
        }

        // Global admins can access any team
        if (_currentUserService.IsGlobalAdmin)
        {
            return Task.FromResult(true);
        }

        // Check team access and role
        var canAccess = _currentUserService.CanAccessTeam(teamId) && 
                       _currentUserService.HasMinimumTeamRole(minimumRole);

        return Task.FromResult(canAccess);
    }

    public Task<bool> IsTeamOwnerAsync(Guid teamId)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Task.FromResult(false);
        }

        // Global admins are considered owners of all teams
        if (_currentUserService.IsGlobalAdmin)
        {
            return Task.FromResult(true);
        }

        // Check if user can access the team and is the owner
        var isOwner = _currentUserService.CanAccessTeam(teamId) && _currentUserService.HasMinimumTeamRole(TeamRole.TeamOwner);
        return Task.FromResult(isOwner);
    }

    public Task<bool> IsTeamAdminAsync(Guid teamId)
    {
        return CanAccessTeamAsync(teamId, TeamRole.TeamAdmin);
    }

    public Task RequireResourceAccessAsync<T>(T resource, TeamRole minimumRole = TeamRole.TeamMember) where T : ITeamResource
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }

        return RequireTeamAccessAsync(resource.TeamId, minimumRole);
    }

    public Task<bool> CanAccessResourceAsync<T>(T resource, TeamRole minimumRole = TeamRole.TeamMember) where T : ITeamResource
    {
        return resource == null ? Task.FromResult(false) : CanAccessTeamAsync(resource.TeamId, minimumRole);
    }

    public async Task<bool> CanManageTeamAsync(Guid teamId, Guid userId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        return userTeam != null && 
               (userTeam.Role == TeamRole.TeamOwner || 
                userTeam.Role == TeamRole.TeamAdmin);
    }

    public async Task<bool> CanManageRegistrationWindowAsync(Guid teamId, Guid userId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        return userTeam != null && 
               (userTeam.Role == TeamRole.TeamOwner || 
                userTeam.Role == TeamRole.TeamAdmin);
    }

    public async Task<bool> CanManageRegistrationAsync(Guid teamId, Guid userId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        return userTeam != null && 
               (userTeam.Role == TeamRole.TeamOwner || 
                userTeam.Role == TeamRole.TeamAdmin);
    }

    public async Task<bool> IsTeamOwnerAsync(Guid teamId, Guid userId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        return userTeam != null && userTeam.Role == TeamRole.TeamOwner;
    }

    public async Task<bool> IsTeamAdminAsync(Guid teamId, Guid userId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        return userTeam != null && userTeam.Role == TeamRole.TeamAdmin;
    }

    public async Task<bool> IsTeamMemberAsync(Guid teamId, Guid userId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        return userTeam != null && userTeam.Role == TeamRole.TeamMember;
    }

    public async Task<TeamRole> GetUserTeamRoleAsync(Guid teamId, Guid userId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        return userTeam?.Role ?? TeamRole.TeamMember;
    }
} 