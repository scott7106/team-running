using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Services;

/// <summary>
/// Service for handling tenant switching operations.
/// Provides functionality to retrieve available tenants for the current user.
/// </summary>
public class TenantSwitcherService : ITenantSwitcherService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<TenantSwitcherService> _logger;

    public TenantSwitcherService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<TenantSwitcherService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantDto>> GetUserTenantsAsync()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userId = _currentUserService.UserId;

        var userTeams = await _context.UserTeams
            .Where(ut => ut.UserId == userId && ut.IsActive && ut.Team!.Status == Domain.Entities.TeamStatus.Active)
            .Include(ut => ut.Team)
            .Select(ut => ut.Team!)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var tenantDtos = userTeams.Select(team => new TenantDto
        {
            TeamId = team.Id,
            TeamName = team.Name,
            Subdomain = team.Subdomain,
            PrimaryColor = team.PrimaryColor,
            SecondaryColor = team.SecondaryColor
        }).ToList();

        _logger.LogInformation("Retrieved {Count} tenants for user {UserId}", tenantDtos.Count, userId);

        return tenantDtos;
    }

    public async Task<SubdomainDto> GetThemeInfoByDomainAsync(string subdomain)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(x => x.Subdomain == subdomain);

        _logger.LogInformation("Retrieved theme data for {subdomain}", subdomain);

        //this endpoint should never fail, so we return a default value if the team is not found
        return new SubdomainDto {
            TeamId = team?.Id ?? Guid.Empty,
            TeamName = team?.Name ?? string.Empty,
            Subdomain = team?.Subdomain ?? string.Empty,
            PrimaryColor = team?.PrimaryColor ?? "#3B82F6",
            SecondaryColor = team?.SecondaryColor ?? "#D1FAE5"
        };
    }

    public async Task<IEnumerable<SubdomainDto>> GetThemeInfoByIdsAsync(IEnumerable<Guid> teamIds)
    {
        var teamIdList = teamIds.ToList();
        if (!teamIdList.Any())
        {
            return Enumerable.Empty<SubdomainDto>();
        }

        var teams = await _context.Teams
            .Where(t => teamIdList.Contains(t.Id) && t.Status == Domain.Entities.TeamStatus.Active && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var result = teams.Select(team => new SubdomainDto
        {
            TeamId = team.Id,
            TeamName = team.Name,
            Subdomain = team.Subdomain,
            PrimaryColor = team.PrimaryColor ?? "#3B82F6",
            SecondaryColor = team.SecondaryColor ?? "#D1FAE5",
            LogoUrl = team.LogoUrl ?? string.Empty
        }).ToList();

        _logger.LogInformation("Retrieved theme data for {Count} teams", result.Count);

        return result;
    }
} 