using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Services;

/// <summary>
/// Implementation of site registration service for public team registration.
/// Handles public team creation and subdomain validation without authentication requirements.
/// Uses TeamManager for core domain operations and manages user creation internally.
/// </summary>
public class SiteRegistrationService : ISiteRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITeamManager _teamManager;
    private readonly ILogger<SiteRegistrationService> _logger;

    public SiteRegistrationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ITeamManager teamManager,
        ILogger<SiteRegistrationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _teamManager = teamManager;
        _logger = logger;
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain, nameof(subdomain));

        try
        {
            return await _teamManager.IsSubdomainAvailableAsync(subdomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subdomain availability for {Subdomain}", subdomain);
            throw;
        }
    }

    public async Task<PublicTeamCreationResultDto> CreateTeamWithNewOwnerAsync(CreateTeamWithNewOwnerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            // Convert DTO to domain request
            var request = new CreateTeamWithNewOwnerRequest
            {
                Name = dto.Name,
                Subdomain = dto.Subdomain,
                OwnerEmail = dto.OwnerEmail,
                OwnerFirstName = dto.OwnerFirstName,
                OwnerLastName = dto.OwnerLastName,
                OwnerPassword = dto.OwnerPassword,
                Tier = dto.Tier,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                ExpiresOn = dto.ExpiresOn
            };

            // Use TeamManager to create team with new owner
            var (user, team) = await _teamManager.CreateTeamWithNewOwnerAsync(request);

            _logger.LogInformation("Created team {TeamId} with new owner {UserId} via public site registration", team.Id, user.Id);

            // Return the result for the controller
            return new PublicTeamCreationResultDto
            {
                TeamId = team.Id,
                TeamName = team.Name,
                TeamSubdomain = team.Subdomain,
                RedirectUrl = $"https://{team.Subdomain}.teamstride.com/team"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team with new owner via public registration for {Email}", dto.OwnerEmail);
            throw;
        }
    }
} 