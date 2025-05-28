using Microsoft.Extensions.Logging;
using TeamStride.Domain.Interfaces;
using TeamStride.Application.Teams.Services;

namespace TeamStride.Infrastructure.Services;

public class CurrentTeamService : ICurrentTeamService
{
    private readonly ILogger<CurrentTeamService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceProvider _serviceProvider;
    private Guid? _currentTeamId;
    private string? _currentSubdomain;

    public CurrentTeamService(
        ILogger<CurrentTeamService> logger,
        ICurrentUserService currentUserService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _currentUserService = currentUserService;
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

    public void SetTeamId(Guid teamId)
    {
        _currentTeamId = teamId;
        _logger.LogInformation("Current team set to {TeamId}", teamId);
    }

    public void SetTeamSubdomain(string subdomain)
    {
        _currentSubdomain = subdomain;
        _logger.LogInformation("Current team subdomain set to {Subdomain}", subdomain);
        // Note: The actual team ID should be resolved from the database
        // This will be implemented when we add the team resolution middleware
    }

    public async Task<bool> SetTeamFromSubdomainAsync(string subdomain)
    {
        try
        {
            // Use service locator pattern to avoid circular dependency
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
            // Check if user is authenticated
            if (!_currentUserService.IsAuthenticated)
            {
                _logger.LogDebug("User not authenticated, cannot set team from JWT claims");
                return false;
            }

            // Get team ID from JWT claims
            var teamIdFromClaims = _currentUserService.TeamId;
            if (!teamIdFromClaims.HasValue)
            {
                _logger.LogDebug("No team ID found in JWT claims");
                return false;
            }

            // Global admins can access any team, so we don't validate team membership for them
            if (_currentUserService.IsGlobalAdmin)
            {
                _currentTeamId = teamIdFromClaims.Value;
                _logger.LogInformation("Global admin team context set to {TeamId}", teamIdFromClaims.Value);
                return true;
            }

            // For regular users, validate they have access to the team
            if (_currentUserService.CanAccessTeam(teamIdFromClaims.Value))
            {
                _currentTeamId = teamIdFromClaims.Value;
                _logger.LogInformation("Team context set from JWT claims to {TeamId}", teamIdFromClaims.Value);
                return true;
            }

            _logger.LogWarning("User {UserId} does not have access to team {TeamId} from JWT claims", 
                _currentUserService.UserId, teamIdFromClaims.Value);
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
} 