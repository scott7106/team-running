using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TeamStride.Domain.Interfaces;
using TeamStride.Domain.Entities;
using TeamStride.Application.Teams.Services;
using System.Text.Json;

namespace TeamStride.Infrastructure.Services;

public class CurrentTeamService : ICurrentTeamService
{
    private readonly ILogger<CurrentTeamService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private Guid? _currentTeamId;
    private string? _currentSubdomain;

    public CurrentTeamService(
        ILogger<CurrentTeamService> logger,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public Guid TeamId
    {
        get
        {
            if (!_currentTeamId.HasValue)
            {
                throw new InvalidOperationException("Current team is not set");
            }
            return _currentTeamId.Value;
        }
    }

    public string? GetSubdomain => _currentSubdomain;

    public bool IsTeamSet => _currentTeamId.HasValue;

    // Parse team memberships from JWT claims
    public List<TeamMembershipInfo> GetTeamMemberships()
    {
        var memberships = new List<TeamMembershipInfo>();
        
        if (!IsAuthenticated)
        {
            return memberships;
        }

        try
        {
            var teamMembershipsJson = _httpContextAccessor.HttpContext?.User?.FindFirst("team_memberships")?.Value;
            if (string.IsNullOrEmpty(teamMembershipsJson))
            {
                return memberships;
            }

            var teamMembershipDtos = JsonSerializer.Deserialize<List<dynamic>>(teamMembershipsJson);
            if (teamMembershipDtos == null) return memberships;

            foreach (var dto in teamMembershipDtos)
            {
                var jsonElement = (JsonElement)dto;
                
                if (jsonElement.TryGetProperty("TeamId", out var teamIdProp) &&
                    jsonElement.TryGetProperty("TeamSubdomain", out var subdomainProp) &&
                    jsonElement.TryGetProperty("TeamRole", out var roleProp) &&
                    jsonElement.TryGetProperty("MemberType", out var memberTypeProp) &&
                    Guid.TryParse(teamIdProp.GetString(), out var teamId) &&
                    Enum.TryParse<TeamRole>(roleProp.GetString(), out var teamRole) &&
                    Enum.TryParse<MemberType>(memberTypeProp.GetString(), out var memberType))
                {
                    memberships.Add(new TeamMembershipInfo(
                        teamId,
                        subdomainProp.GetString() ?? string.Empty,
                        teamRole,
                        memberType));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse team memberships from JWT claims");
        }

        return memberships;
    }

    // Get current team membership based on current subdomain
    private TeamMembershipInfo? GetCurrentTeamMembership()
    {
        if (string.IsNullOrEmpty(_currentSubdomain))
        {
            return null;
        }

        var memberships = GetTeamMemberships();
        return memberships.FirstOrDefault(m => m.TeamSubdomain.Equals(_currentSubdomain, StringComparison.OrdinalIgnoreCase));
    }

    // Team Role Properties from Current Team Membership
    public TeamRole? CurrentTeamRole
    {
        get
        {
            if (IsGlobalAdmin) return TeamRole.TeamOwner; // Global admins have full permissions
            return GetCurrentTeamMembership()?.TeamRole;
        }
    }

    public MemberType? CurrentMemberType
    {
        get
        {
            if (IsGlobalAdmin) return MemberType.Coach; // Default for global admins
            return GetCurrentTeamMembership()?.MemberType;
        }
    }

    // Helper Properties for Current Team
    public bool IsTeamOwner => CurrentTeamRole == Domain.Entities.TeamRole.TeamOwner;
    public bool IsTeamAdmin => CurrentTeamRole == Domain.Entities.TeamRole.TeamAdmin;
    public bool IsTeamMember => CurrentTeamRole == Domain.Entities.TeamRole.TeamMember;

    // Team Context Management Methods
    public void SetTeamId(Guid teamId)
    {
        _currentTeamId = teamId;
        _logger.LogInformation("Current team set to {TeamId}", teamId);
    }

    public void SetTeamSubdomain(string subdomain)
    {
        _currentSubdomain = subdomain;
        _logger.LogInformation("Current team subdomain set to {Subdomain}", subdomain);
    }

    public async Task<bool> SetTeamFromSubdomainAsync(string subdomain)
    {
        try
        {
            var standardTeamService = _serviceProvider.GetService(typeof(IStandardTeamService)) as IStandardTeamService;
            if (standardTeamService == null)
            {
                _logger.LogWarning("StandardTeamService not available for team resolution");
                return false;
            }

            var team = await standardTeamService.GetTeamBySubdomainAsync(subdomain);
            if (team != null)
            {
                _currentTeamId = team.Id;
                _currentSubdomain = subdomain;
                _logger.LogInformation("Team context set from subdomain {Subdomain} to team {TeamId}", subdomain, team.Id);
                return true;
            }

            _logger.LogWarning("No team found for subdomain: {Subdomain}", subdomain);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving team from subdomain: {Subdomain}", subdomain);
            return false;
        }
    }

    public bool SetTeamFromJwtClaims()
    {
        try
        {
            if (!IsAuthenticated)
            {
                _logger.LogDebug("User not authenticated, cannot set team from JWT claims");
                return false;
            }

            // For global admins, we don't set team from claims as they can access any team
            if (IsGlobalAdmin)
            {
                _logger.LogDebug("Global admin user - team context determined by subdomain");
                return true;
            }

            // For regular users, the current team is determined by subdomain matching
            // This method is now primarily used for validation
            if (!string.IsNullOrEmpty(_currentSubdomain))
            {
                var currentMembership = GetCurrentTeamMembership();
                if (currentMembership != null)
                {
                    _currentTeamId = currentMembership.TeamId;
                    _logger.LogInformation("Team context validated from JWT claims for subdomain {Subdomain} to team {TeamId}", _currentSubdomain, currentMembership.TeamId);
                    return true;
                }
            }

            _logger.LogWarning("No matching team membership found for current subdomain {Subdomain}", _currentSubdomain);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting team from JWT claims");
            return false;
        }
    }

    public void ClearTeam()
    {
        _currentTeamId = null;
        _currentSubdomain = null;
        _logger.LogInformation("Current team cleared");
    }

    // Authorization Methods
    public bool CanAccessCurrentTeam()
    {
        if (!IsTeamSet) return false;
        return CanAccessTeam(TeamId);
    }

    public bool HasMinimumTeamRole(TeamRole minimumRole)
    {
        if (IsGlobalAdmin) return true;
        if (!CurrentTeamRole.HasValue) return false;

        var roleHierarchy = new Dictionary<TeamRole, int>
        {
            { Domain.Entities.TeamRole.TeamOwner, 1 },
            { Domain.Entities.TeamRole.TeamAdmin, 2 },
            { Domain.Entities.TeamRole.TeamMember, 3 }
        };

        return roleHierarchy[CurrentTeamRole.Value] <= roleHierarchy[minimumRole];
    }

    public bool CanAccessTeam(Guid teamId)
    {
        if (IsGlobalAdmin) return true;

        // Check if user has membership in the specified team
        var memberships = GetTeamMemberships();
        return memberships.Any(m => m.TeamId == teamId);
    }

    // Private Helper Methods
    private bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    private bool IsGlobalAdmin
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("is_global_admin");
            return claim != null && bool.TryParse(claim.Value, out var isGlobalAdmin) && isGlobalAdmin;
        }
    }
} 