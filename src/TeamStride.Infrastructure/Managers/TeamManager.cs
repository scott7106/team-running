using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Managers;

/// <summary>
/// Domain service implementation for core team management operations.
/// Handles team creation, subdomain management, and team retrieval without authentication concerns.
/// </summary>
public class TeamManager : ITeamManager
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeamManager> _logger;

    // Regex for validating subdomain format: 3-63 characters, lowercase letters, numbers, hyphens
    // Cannot start or end with hyphen
    private static readonly Regex SubdomainRegex = new(@"^[a-z0-9]([a-z0-9\-]{1,61}[a-z0-9])?$", RegexOptions.Compiled);

    public TeamManager(
        ApplicationDbContext context,
        ILogger<TeamManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTeamId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain);

        var normalizedSubdomain = NormalizeSubdomain(subdomain);
        
        var query = _context.Teams
            .IgnoreQueryFilters() // Check all teams, including soft-deleted ones
            .Where(t => t.Subdomain == normalizedSubdomain && !t.IsDeleted);

        if (excludeTeamId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTeamId.Value);
        }

        var exists = await query.AnyAsync();
        return !exists;
    }

    public async Task<Team> GetTeamBySubdomainAsync(string subdomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain);

        var normalizedSubdomain = NormalizeSubdomain(subdomain);
        
        var team = await _context.Teams
            .IgnoreQueryFilters() // Allow public access without authorization
            .Include(t => t.Users) // Include users for potential access checks by calling services
            .FirstOrDefaultAsync(t => t.Subdomain == normalizedSubdomain && !t.IsDeleted);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with subdomain '{subdomain}' not found");
        }

        return team;
    }

    public async Task<Team> CreateTeamAsync(CreateTeamRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subdomain);

        // Validate subdomain format
        if (!IsValidSubdomainFormat(request.Subdomain))
        {
            throw new InvalidOperationException($"Invalid subdomain format: '{request.Subdomain}'. " +
                "Subdomain must be 3-63 characters, lowercase letters, numbers, and hyphens only. " +
                "Cannot start or end with hyphen.");
        }

        var normalizedSubdomain = NormalizeSubdomain(request.Subdomain);

        // Validate subdomain availability
        if (!await IsSubdomainAvailableAsync(normalizedSubdomain))
        {
            throw new InvalidOperationException($"Subdomain '{normalizedSubdomain}' is already taken");
        }

        // Validate that the owner exists in the database
        var ownerExists = await _context.Users.AnyAsync(u => u.Id == request.OwnerId);
        if (!ownerExists)
        {
            throw new InvalidOperationException($"Owner with ID '{request.OwnerId}' not found");
        }

        // Create the team (transaction management is handled by the calling service)
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Subdomain = normalizedSubdomain,
            OwnerId = request.OwnerId,
            Tier = request.Tier,
            Status = request.Status,
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            ExpiresOn = request.ExpiresOn,
            LogoUrl = request.LogoUrl,
            CreatedOn = DateTime.UtcNow
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        // Create the owner relationship in UserTeam
        var userTeam = new UserTeam
        {
            UserId = request.OwnerId,
            TeamId = team.Id,
            Role = TeamRole.TeamOwner,
            MemberType = MemberType.Coach,
            IsActive = true,
            IsDefault = true, // New teams are set as default - calling service can override this
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        _context.UserTeams.Add(userTeam);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created team {TeamId} '{TeamName}' with subdomain '{Subdomain}' for owner {OwnerId}", 
            team.Id, team.Name, team.Subdomain, request.OwnerId);

        return team;
    }

    public bool IsValidSubdomainFormat(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;

        var normalized = NormalizeSubdomain(subdomain);
        
        // Check against regex pattern
        if (!SubdomainRegex.IsMatch(normalized))
            return false;

        // Additional business rules
        if (normalized.Length < 3 || normalized.Length > 63)
            return false;

        // Check for reserved subdomains
        var reservedSubdomains = new[]
        {
            "www", "api", "admin", "mail", "ftp", "support", "help", "blog", "app", "mobile",
            "test", "staging", "dev", "demo", "beta", "alpha", "cdn", "static", "assets",
            "teamstride", "team-stride"
        };

        if (reservedSubdomains.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public string NormalizeSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return string.Empty;

        return subdomain.Trim().ToLowerInvariant();
    }

} 