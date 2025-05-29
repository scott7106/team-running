using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TeamStride.Domain.Interfaces;
using TeamStride.Domain.Entities;
using TeamStride.Application.Teams.Services;

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

    // Team Role Properties from JWT Claims
    public TeamRole? TeamRole
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("team_role");
            return claim != null && Enum.TryParse<TeamRole>(claim.Value, out var role) ? role : null;
        }
    }

    public MemberType? MemberType
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("member_type");
            return claim != null && Enum.TryParse<MemberType>(claim.Value, out var memberType) ? memberType : null;
        }
    }

    // Helper Properties for Current Team
    public bool IsTeamOwner => TeamRole == Domain.Entities.TeamRole.TeamOwner;
    public bool IsTeamAdmin => TeamRole == Domain.Entities.TeamRole.TeamAdmin;
    public bool IsTeamMember => TeamRole == Domain.Entities.TeamRole.TeamMember;

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
            var teamManagementService = _serviceProvider.GetService(typeof(ITeamService)) as ITeamService;
            if (teamManagementService == null)
            {
                _logger.LogWarning("TeamService not available for team resolution");
                return false;
            }

            var team = await teamManagementService.GetTeamBySubdomainAsync(subdomain);
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

            var teamIdFromClaims = GetTeamIdFromClaims();
            if (!teamIdFromClaims.HasValue)
            {
                _logger.LogDebug("No team ID found in JWT claims");
                return false;
            }

            if (IsGlobalAdmin || CanAccessTeam(teamIdFromClaims.Value))
            {
                _currentTeamId = teamIdFromClaims.Value;
                _logger.LogInformation("Team context set from JWT claims to {TeamId}", teamIdFromClaims.Value);
                return true;
            }

            _logger.LogWarning("User does not have access to team {TeamId} from JWT claims", teamIdFromClaims.Value);
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
        if (!TeamRole.HasValue) return false;

        var roleHierarchy = new Dictionary<TeamRole, int>
        {
            { Domain.Entities.TeamRole.TeamOwner, 1 },
            { Domain.Entities.TeamRole.TeamAdmin, 2 },
            { Domain.Entities.TeamRole.TeamMember, 3 }
        };

        return roleHierarchy[TeamRole.Value] <= roleHierarchy[minimumRole];
    }

    public bool CanAccessTeam(Guid teamId)
    {
        if (IsGlobalAdmin) return true;

        var teamIdFromClaims = GetTeamIdFromClaims();
        return teamIdFromClaims.HasValue && teamIdFromClaims.Value == teamId;
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

    private Guid? GetTeamIdFromClaims()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("team_id");
        return claim != null && Guid.TryParse(claim.Value, out var teamId) ? teamId : null;
    }
} 