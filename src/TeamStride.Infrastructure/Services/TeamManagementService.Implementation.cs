using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Email;

namespace TeamStride.Infrastructure.Services;

public partial class TeamManagementService
{
    public async Task<TeamManagementDto> CreateTeamAsync(CreateTeamDto dto)
    {
        await EnsureGlobalAdminAsync();

        // Validate subdomain
        if (!await IsSubdomainAvailableAsync(dto.Subdomain))
        {
            throw new InvalidOperationException($"Subdomain '{dto.Subdomain}' is not available");
        }

        // Find or create the owner user
        var owner = await _userManager.FindByEmailAsync(dto.OwnerEmail);
        var isNewUser = owner == null;

        if (isNewUser)
        {
            owner = new ApplicationUser
            {
                UserName = dto.OwnerEmail,
                Email = dto.OwnerEmail,
                FirstName = dto.OwnerFirstName ?? "Team",
                LastName = dto.OwnerLastName ?? "Owner",
                EmailConfirmed = false,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(owner);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create owner user: {errors}");
            }
        }

        // Create the team
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Subdomain = dto.Subdomain,
            PrimaryColor = dto.PrimaryColor,
            SecondaryColor = dto.SecondaryColor,
            Status = TeamStatus.Active,
            Tier = dto.Tier
        };

        _context.Teams.Add(team);

        // Create the owner relationship
        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = owner.Id,
            TeamId = team.Id,
            Role = TeamRole.Host,
            IsDefault = true,
            IsActive = true,
            JoinedOn = DateTime.UtcNow
        };

        _context.UserTeams.Add(userTeam);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamName} created with ID {TeamId} by user {UserId}", 
            team.Name, team.Id, _currentUserService.UserId);

        return await GetTeamByIdAsync(team.Id);
    }

    public async Task<TeamManagementDto> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto)
    {
        await EnsureCanManageTeamAsync(teamId);

        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            team.Name = dto.Name;
        }

        if (dto.Status.HasValue)
        {
            team.Status = dto.Status.Value;
        }

        if (dto.ExpiresOn.HasValue)
        {
            team.ExpiresOn = dto.ExpiresOn.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} updated by user {UserId}", teamId, _currentUserService.UserId);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task DeleteTeamAsync(Guid teamId)
    {
        await EnsureCanDeleteTeamAsync(teamId);

        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Soft delete the team
        team.IsDeleted = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} deleted by user {UserId}", teamId, _currentUserService.UserId);
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;

        subdomain = subdomain.ToLowerInvariant();

        // Check blacklist
        if (BlacklistedSubdomains.Contains(subdomain))
            return false;

        // Check if already taken
        var exists = await _context.Teams
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Subdomain == subdomain);

        return !exists;
    }

    public async Task<TeamManagementDto> UpdateSubdomainAsync(Guid teamId, string newSubdomain)
    {
        await EnsureGlobalAdminAsync(); // Only global admins can change subdomains

        if (!await IsSubdomainAvailableAsync(newSubdomain))
        {
            throw new InvalidOperationException($"Subdomain '{newSubdomain}' is not available");
        }

        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        var oldSubdomain = team.Subdomain;
        team.Subdomain = newSubdomain;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} subdomain changed from {OldSubdomain} to {NewSubdomain} by user {UserId}", 
            teamId, oldSubdomain, newSubdomain, _currentUserService.UserId);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task<TeamTierLimitsDto> GetTierLimitsAsync(TeamTier tier)
    {
        return tier switch
        {
            TeamTier.Free => new TeamTierLimitsDto
            {
                Tier = TeamTier.Free,
                MaxAthletes = 7,
                MaxAdmins = 2,
                MaxCoaches = 2,
                AllowCustomBranding = false,
                AllowAdvancedReporting = false
            },
            TeamTier.Standard => new TeamTierLimitsDto
            {
                Tier = TeamTier.Standard,
                MaxAthletes = 30,
                MaxAdmins = 5,
                MaxCoaches = 5,
                AllowCustomBranding = true,
                AllowAdvancedReporting = false
            },
            TeamTier.Premium => new TeamTierLimitsDto
            {
                Tier = TeamTier.Premium,
                MaxAthletes = int.MaxValue, // Unlimited
                MaxAdmins = int.MaxValue,
                MaxCoaches = int.MaxValue,
                AllowCustomBranding = true,
                AllowAdvancedReporting = true
            },
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };
    }

    public async Task<bool> CanAddAthleteAsync(Guid teamId)
    {
        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
            return false;

        var limits = await GetTierLimitsAsync(team.Tier);
        if (limits.MaxAthletes == int.MaxValue)
            return true;

        var currentAthleteCount = await _context.UserTeams
            .CountAsync(ut => ut.TeamId == teamId && ut.IsActive && ut.Role == TeamRole.Athlete);

        return currentAthleteCount < limits.MaxAthletes;
    }

    // Additional authorization helper methods
    private async Task EnsureCanManageTeamAsync(Guid teamId)
    {
        if (_currentUserService.IsGlobalAdmin)
            return;

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

        if (userTeam == null || (userTeam.Role != TeamRole.Host && userTeam.Role != TeamRole.Admin))
        {
            throw new UnauthorizedAccessException("Team management access required");
        }
    }

    private async Task EnsureCanDeleteTeamAsync(Guid teamId)
    {
        if (_currentUserService.IsGlobalAdmin)
            return;

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

        if (userTeam == null || userTeam.Role != TeamRole.Host)
        {
            throw new UnauthorizedAccessException("Team owner access required to delete team");
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
} 