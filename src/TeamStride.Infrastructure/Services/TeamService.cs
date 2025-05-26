using Microsoft.Extensions.Logging;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly ILogger<TeamService> _logger;
    private Guid? _currentTeamId;
    private string? _currentSubdomain;

    public TeamService(ILogger<TeamService> logger)
    {
        _logger = logger;
    }

    public Guid CurrentTeamId
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

    public string? CurrentTeamSubdomain => _currentSubdomain;

    public void SetCurrentTeam(Guid teamId)
    {
        _currentTeamId = teamId;
        _logger.LogInformation("Current team set to {TeamId}", teamId);
    }

    public void SetCurrentTeam(string subdomain)
    {
        _currentSubdomain = subdomain;
        _logger.LogInformation("Current team subdomain set to {Subdomain}", subdomain);
        // Note: The actual team ID should be resolved from the database
        // This will be implemented when we add the team resolution middleware
    }

    public void ClearCurrentTeam()
    {
        _currentTeamId = null;
        _currentSubdomain = null;
        _logger.LogInformation("Current team cleared");
    }
} 