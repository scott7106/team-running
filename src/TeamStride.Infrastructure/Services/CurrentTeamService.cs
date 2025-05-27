using Microsoft.Extensions.Logging;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Services;

public class CurrentTeamService : ICurrentTeamService
{
    private readonly ILogger<CurrentTeamService> _logger;
    private Guid? _currentTeamId;
    private string? _currentSubdomain;

    public CurrentTeamService(ILogger<CurrentTeamService> logger)
    {
        _logger = logger;
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

    public void ClearTeam()
    {
        _currentTeamId = null;
        _currentSubdomain = null;
        _logger.LogInformation("Current team cleared");
    }
} 