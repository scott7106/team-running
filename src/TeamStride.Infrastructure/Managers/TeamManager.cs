using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TeamManager> _logger;

    // Regex for validating subdomain format: 3-63 characters, lowercase letters, numbers, hyphens
    // Cannot start or end with hyphen
    private static readonly Regex SubdomainRegex = new(@"^[a-z0-9]([a-z0-9\-]{1,61}[a-z0-9])?$", RegexOptions.Compiled);

    public TeamManager(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TeamManager> logger)
    {
        _context = context;
        _userManager = userManager;
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

        // Validate that the owner exists
        var owner = await _userManager.FindByIdAsync(request.OwnerId.ToString());
        if (owner == null)
        {
            throw new InvalidOperationException($"Owner with ID '{request.OwnerId}' not found");
        }

        // Use execution strategy for transaction resilience
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create the team
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

                // Create or update the owner relationship in UserTeam
                var existingUserTeam = await _context.UserTeams
                    .FirstOrDefaultAsync(ut => ut.UserId == request.OwnerId && ut.TeamId == team.Id);

                if (existingUserTeam != null)
                {
                    // Update existing relationship
                    existingUserTeam.Role = TeamRole.TeamOwner;
                    existingUserTeam.MemberType = MemberType.Coach;
                    existingUserTeam.IsActive = true;
                    existingUserTeam.ModifiedOn = DateTime.UtcNow;
                }
                else
                {
                    // Create new relationship
                    var userTeam = new UserTeam
                    {
                        UserId = request.OwnerId,
                        TeamId = team.Id,
                        Role = TeamRole.TeamOwner,
                        MemberType = MemberType.Coach,
                        IsActive = true,
                        IsDefault = owner.DefaultTeamId == null, // Set as default if user has no default team
                        JoinedOn = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    };

                    _context.UserTeams.Add(userTeam);
                }

                // Update user's default team if they don't have one
                if (owner.DefaultTeamId == null)
                {
                    owner.DefaultTeamId = team.Id;
                    await _userManager.UpdateAsync(owner);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created team {TeamId} '{TeamName}' with subdomain '{Subdomain}' for owner {OwnerId}", 
                    team.Id, team.Name, team.Subdomain, request.OwnerId);

                return team;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
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

    public async Task<(ApplicationUser User, Team Team)> CreateTeamWithNewOwnerAsync(CreateTeamWithNewOwnerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subdomain);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OwnerEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OwnerFirstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OwnerLastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OwnerPassword);

        // Validate subdomain availability
        if (!await IsSubdomainAvailableAsync(request.Subdomain))
        {
            throw new InvalidOperationException($"Subdomain '{request.Subdomain}' is already taken");
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.OwnerEmail);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email '{request.OwnerEmail}' already exists");
        }

        // Use execution strategy for transaction resilience
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create the new user first
                var newUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = request.OwnerEmail,
                    Email = request.OwnerEmail,
                    FirstName = request.OwnerFirstName,
                    LastName = request.OwnerLastName,
                    EmailConfirmed = true, // Auto-confirm for team owners
                    CreatedOn = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, request.OwnerPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // Create team using existing CreateTeamAsync method
                var createTeamRequest = new CreateTeamRequest
                {
                    Name = request.Name,
                    Subdomain = request.Subdomain,
                    OwnerId = newUser.Id,
                    Tier = request.Tier,
                    Status = TeamStatus.Active,
                    PrimaryColor = request.PrimaryColor,
                    SecondaryColor = request.SecondaryColor,
                    ExpiresOn = request.ExpiresOn
                };

                var team = await CreateTeamAsync(createTeamRequest);
                await transaction.CommitAsync();

                _logger.LogInformation("Created team {TeamId} with new owner {UserId} via TeamManager", team.Id, newUser.Id);

                return (newUser, team);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
} 