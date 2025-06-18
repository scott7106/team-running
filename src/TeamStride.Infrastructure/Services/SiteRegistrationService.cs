using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Configuration;
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
    private readonly AppConfiguration _appConfiguration;

    public SiteRegistrationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ITeamManager teamManager,
        ILogger<SiteRegistrationService> logger,
        IOptions<AppConfiguration> appConfiguration)
    {
        _context = context;
        _userManager = userManager;
        _teamManager = teamManager;
        _logger = logger;
        _appConfiguration = appConfiguration.Value;
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTeamId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain, nameof(subdomain));

        try
        {
            return await _teamManager.IsSubdomainAvailableAsync(subdomain, excludeTeamId);
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
            // Validate subdomain availability first
            if (!await _teamManager.IsSubdomainAvailableAsync(dto.Subdomain))
            {
                throw new InvalidOperationException($"Subdomain '{dto.Subdomain}' is already taken");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.OwnerEmail);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"User with email '{dto.OwnerEmail}' already exists");
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
                        UserName = dto.OwnerEmail,
                        Email = dto.OwnerEmail,
                        FirstName = dto.OwnerFirstName,
                        LastName = dto.OwnerLastName,
                        EmailConfirmed = true, // Auto-confirm for team owners
                        IsActive = true,
                        Status = UserStatus.Active,
                        CreatedOn = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(newUser, dto.OwnerPassword);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }

                    // Create team using TeamManager
                    var createTeamRequest = new CreateTeamRequest
                    {
                        Name = dto.Name,
                        Subdomain = dto.Subdomain,
                        OwnerId = newUser.Id,
                        Tier = dto.Tier,
                        Status = TeamStatus.Active,
                        PrimaryColor = dto.PrimaryColor,
                        SecondaryColor = dto.SecondaryColor,
                        ExpiresOn = dto.ExpiresOn
                    };

                    var team = await _teamManager.CreateTeamAsync(createTeamRequest);

                    // Set the team as the user's default team
                    newUser.DefaultTeamId = team.Id;
                    await _userManager.UpdateAsync(newUser);

                    await transaction.CommitAsync();

                    _logger.LogInformation("Created team {TeamId} with new owner {UserId} via public site registration", team.Id, newUser.Id);

                    // Return the result for the controller
                    return new PublicTeamCreationResultDto
                    {
                        TeamId = team.Id,
                        TeamName = team.Name,
                        TeamSubdomain = team.Subdomain,
                        RedirectUrl = _appConfiguration.TeamUrl.Replace("{team}", team.Subdomain)
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team with new owner via public registration for {Email}", dto.OwnerEmail);
            throw;
        }
    }

    public async Task<PublicTeamCreationResultDto> CreateTeamWithExistingOwnerAsync(CreateTeamWithExistingOwnerDto dto, Guid currentUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            // Use execution strategy for transaction resilience
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create team request using the current user as owner
                    var createTeamRequest = new CreateTeamRequest
                    {
                        Name = dto.Name,
                        Subdomain = dto.Subdomain,
                        OwnerId = currentUserId, // Use the authenticated user
                        Tier = dto.Tier,
                        Status = TeamStatus.Active,
                        PrimaryColor = dto.PrimaryColor,
                        SecondaryColor = dto.SecondaryColor,
                        ExpiresOn = dto.ExpiresOn
                    };

                    // Use TeamManager to create team with existing owner
                    var team = await _teamManager.CreateTeamAsync(createTeamRequest);

                    // Update user's default team if they don't have one
                    var user = await _userManager.FindByIdAsync(currentUserId.ToString());
                    if (user != null && user.DefaultTeamId == null)
                    {
                        user.DefaultTeamId = team.Id;
                        await _userManager.UpdateAsync(user);
                    }

                    await transaction.CommitAsync();

                    _logger.LogInformation("Created team {TeamId} with existing owner {UserId} via public site registration", team.Id, currentUserId);

                    // Return the result for the controller
                    return new PublicTeamCreationResultDto
                    {
                        TeamId = team.Id,
                        TeamName = team.Name,
                        TeamSubdomain = team.Subdomain,
                        RedirectUrl = _appConfiguration.TeamUrl.Replace("{team}", team.Subdomain)
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team with existing owner via public registration for user {UserId}", currentUserId);
            throw;
        }
    }
} 