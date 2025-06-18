using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Services;

/// <summary>
/// Implementation of global admin team management service.
/// All operations require global admin privileges and bypass normal team access restrictions.
/// </summary>
public class GlobalAdminTeamService : IGlobalAdminTeamService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<GlobalAdminTeamService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITeamManager _teamManager;

    public GlobalAdminTeamService(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<GlobalAdminTeamService> logger, 
        ICurrentUserService currentUserService,
        ITeamManager teamManager)
    {
        _context = context;
        _authorizationService = authorizationService;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _teamManager = teamManager;
    }

    public async Task<PaginatedList<GlobalAdminTeamDto>> GetTeamsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null,
        TeamStatus? status = null,
        TeamTier? tier = null,
        DateTime? expiresOnFrom = null,
        DateTime? expiresOnTo = null)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        IQueryable<Team> query = _context.Teams
            .IgnoreQueryFilters() // Bypass global query filters for global admin
            .Where(t => !t.IsDeleted)
            .Include(t => t.Users.Where(ut => ut.Role == TeamRole.TeamOwner))
            .ThenInclude(ut => ut.User);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.ToLower();
            query = query.Where(t => 
                t.Name.ToLower().Contains(search) ||
                t.Subdomain.ToLower().Contains(search) ||
                t.Users.Any(ut => ut.Role == TeamRole.TeamOwner && 
                    (ut.User.Email!.ToLower().Contains(search) ||
                     ut.User.FirstName.ToLower().Contains(search) ||
                     ut.User.LastName.ToLower().Contains(search))));
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        // Apply tier filter
        if (tier.HasValue)
        {
            query = query.Where(t => t.Tier == tier.Value);
        }

        // Apply expiration date filters
        if (expiresOnFrom.HasValue)
        {
            query = query.Where(t => t.ExpiresOn >= expiresOnFrom.Value);
        }

        if (expiresOnTo.HasValue)
        {
            query = query.Where(t => t.ExpiresOn <= expiresOnTo.Value);
        }

        // Order by creation date (newest first)
        query = query.OrderByDescending(t => t.CreatedOn);

        var totalCount = await query.CountAsync();
        var teams = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var teamDtos = teams.Select(team =>
        {
            var owner = team.Users.FirstOrDefault(ut => ut.Role == TeamRole.TeamOwner);
            var dto = _mapper.Map<GlobalAdminTeamDto>(team);
            
            if (owner?.User != null)
            {
                dto.OwnerId = owner.User.Id;
                dto.OwnerEmail = owner.User.Email ?? string.Empty;
                dto.OwnerFirstName = owner.User.FirstName;
                dto.OwnerLastName = owner.User.LastName;
                dto.OwnerDisplayName = $"{owner.User.LastName}, {owner.User.FirstName}";
            }

            // Get statistics
            dto.MemberCount = team.Users.Count(ut => ut.IsActive);
            dto.AdminCount = team.Users.Count(ut => ut.IsActive && 
                (ut.Role == TeamRole.TeamOwner || ut.Role == TeamRole.TeamAdmin));
            dto.AthleteCount = team.Users.Count(ut => ut.IsActive && ut.MemberType == MemberType.Athlete);

            // Check for pending ownership transfers
            dto.HasPendingOwnershipTransfer = _context.OwnershipTransfers
                .Any(ot => ot.TeamId == team.Id && ot.Status == OwnershipTransferStatus.Pending);

            return dto;
        }).ToList();

        return new PaginatedList<GlobalAdminTeamDto>(teamDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedList<DeletedTeamDto>> GetDeletedTeamsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        IQueryable<Team> query = _context.Teams
            .IgnoreQueryFilters() // Bypass global query filters
            .Where(t => t.IsDeleted)
            .Include(t => t.Users.Where(ut => ut.Role == TeamRole.TeamOwner))
            .ThenInclude(ut => ut.User);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.ToLower();
            query = query.Where(t => 
                t.Name.ToLower().Contains(search) ||
                t.Subdomain.ToLower().Contains(search) ||
                t.Users.Any(ut => ut.Role == TeamRole.TeamOwner && 
                    ut.User.Email!.ToLower().Contains(search)));
        }

        // Order by deletion date (newest first)
        query = query.OrderByDescending(t => t.DeletedOn);

        var totalCount = await query.CountAsync();
        var teams = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var deletedTeamDtos = teams.Select(team =>
        {
            var owner = team.Users.FirstOrDefault(ut => ut.Role == TeamRole.TeamOwner);
            var dto = _mapper.Map<DeletedTeamDto>(team);
            
            if (owner?.User != null)
            {
                dto.OwnerId = owner.User.Id;
                dto.OwnerEmail = owner.User.Email ?? string.Empty;
                dto.OwnerDisplayName = $"{owner.User.LastName}, {owner.User.FirstName}";
            }

            // Get deleted by user info
            if (team.DeletedBy.HasValue)
            {
                var deletedByUser = _context.Users.Find(team.DeletedBy.Value);
                dto.DeletedByUserEmail = deletedByUser?.Email;
            }

            return dto;
        }).ToList();

        return new PaginatedList<DeletedTeamDto>(deletedTeamDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<GlobalAdminTeamDto> GetTeamByIdAsync(Guid teamId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var team = await _context.Teams
            .IgnoreQueryFilters() // Bypass global query filters
            .Include(t => t.Users)
            .ThenInclude(ut => ut.User)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        var owner = team.Users.FirstOrDefault(ut => ut.Role == TeamRole.TeamOwner);
        var dto = _mapper.Map<GlobalAdminTeamDto>(team);
        
        if (owner?.User != null)
        {
            dto.OwnerId = owner.User.Id;
            dto.OwnerEmail = owner.User.Email ?? string.Empty;
            dto.OwnerFirstName = owner.User.FirstName;
            dto.OwnerLastName = owner.User.LastName;
            dto.OwnerDisplayName = $"{owner.User.LastName}, {owner.User.FirstName}";
        }

        // Get statistics
        dto.MemberCount = team.Users.Count(ut => ut.IsActive);
        dto.AdminCount = team.Users.Count(ut => ut.IsActive && 
            (ut.Role == TeamRole.TeamOwner || ut.Role == TeamRole.TeamAdmin));
        dto.AthleteCount = team.Users.Count(ut => ut.IsActive && ut.MemberType == MemberType.Athlete);

        // Check for pending ownership transfers
        dto.HasPendingOwnershipTransfer = await _context.OwnershipTransfers
            .AnyAsync(ot => ot.TeamId == team.Id && ot.Status == OwnershipTransferStatus.Pending);

        return dto;
    }

    public async Task<GlobalAdminTeamDto> CreateTeamWithNewOwnerAsync(CreateTeamWithNewOwnerDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

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

                _logger.LogInformation("Created team {TeamId} with new owner {UserId} via global admin", team.Id, newUser.Id);

                return await GetTeamByIdAsync(team.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<GlobalAdminTeamDto> CreateTeamWithExistingOwnerAsync(CreateTeamWithExistingOwnerDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        // Use execution strategy for transaction resilience
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var createTeamRequest = new CreateTeamRequest
                {
                    Name = dto.Name,
                    Subdomain = dto.Subdomain,
                    OwnerId = dto.OwnerId,
                    Tier = dto.Tier,
                    Status = TeamStatus.Active,
                    PrimaryColor = dto.PrimaryColor,
                    SecondaryColor = dto.SecondaryColor,
                    ExpiresOn = dto.ExpiresOn
                };

                var team = await _teamManager.CreateTeamAsync(createTeamRequest);

                // Update user's default team if they don't have one
                var user = await _userManager.FindByIdAsync(dto.OwnerId.ToString());
                if (user != null && user.DefaultTeamId == null)
                {
                    user.DefaultTeamId = team.Id;
                    await _userManager.UpdateAsync(user);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Created team {TeamId} with existing owner {UserId} via global admin", team.Id, dto.OwnerId);

                return await GetTeamByIdAsync(team.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<GlobalAdminTeamDto> UpdateTeamAsync(Guid teamId, GlobalAdminUpdateTeamDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var team = await _context.Teams
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Validate subdomain if it's being changed
        if (!string.IsNullOrWhiteSpace(dto.Subdomain) && dto.Subdomain != team.Subdomain)
        {
            if (!await IsSubdomainAvailableAsync(dto.Subdomain, teamId))
            {
                throw new InvalidOperationException($"Subdomain '{dto.Subdomain}' is already taken");
            }
        }

        // Update properties
        if (!string.IsNullOrWhiteSpace(dto.Name))
            team.Name = dto.Name;

        if (!string.IsNullOrWhiteSpace(dto.Subdomain))
            team.Subdomain = dto.Subdomain;

        if (dto.Status.HasValue)
            team.Status = dto.Status.Value;

        if (dto.Tier.HasValue)
            team.Tier = dto.Tier.Value;

        if (dto.ExpiresOn.HasValue)
            team.ExpiresOn = dto.ExpiresOn.Value;

        if (!string.IsNullOrWhiteSpace(dto.PrimaryColor))
            team.PrimaryColor = dto.PrimaryColor;

        if (!string.IsNullOrWhiteSpace(dto.SecondaryColor))
            team.SecondaryColor = dto.SecondaryColor;

        if (!string.IsNullOrWhiteSpace(dto.LogoUrl))
            team.LogoUrl = dto.LogoUrl;

        team.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated team {TeamId}", teamId);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task DeleteTeamAsync(Guid teamId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var team = await _context.Teams
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        if (team.IsDeleted)
        {
            throw new InvalidOperationException($"Team with ID {teamId} is already deleted");
        }

        // Soft delete the team
        team.IsDeleted = true;
        team.DeletedOn = DateTime.UtcNow;
        team.Status = TeamStatus.Suspended;

        // Deactivate all user-team relationships
        var userTeams = await _context.UserTeams
            .IgnoreQueryFilters()
            .Where(ut => ut.TeamId == teamId)
            .ToListAsync();

        foreach (var userTeam in userTeams)
        {
            userTeam.IsActive = false;
            userTeam.ModifiedOn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft deleted team {TeamId}", teamId);
    }

    public async Task PurgeTeamAsync(Guid teamId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var team = await _context.Teams
            .IgnoreQueryFilters()
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove all user-team relationships
                _context.UserTeams.RemoveRange(team.Users);

                // Remove ownership transfers
                var ownershipTransfers = await _context.OwnershipTransfers
                    .IgnoreQueryFilters()
                    .Where(ot => ot.TeamId == teamId)
                    .ToListAsync();
                _context.OwnershipTransfers.RemoveRange(ownershipTransfers);

                // Remove the team
                _context.Teams.Remove(team);

                // Use the bypass method to avoid soft delete conversion
                await _context.SaveChangesWithoutAuditAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Permanently deleted team {TeamId}", teamId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<GlobalAdminTeamDto> RecoverTeamAsync(Guid teamId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var team = await _context.Teams
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        if (!team.IsDeleted)
        {
            throw new InvalidOperationException($"Team with ID {teamId} is not deleted");
        }

        // Validate subdomain is still available
        if (!await IsSubdomainAvailableAsync(team.Subdomain, teamId))
        {
            throw new InvalidOperationException($"Cannot recover team: subdomain '{team.Subdomain}' is now taken");
        }

        // Recover the team
        team.IsDeleted = false;
        team.DeletedOn = null;
        team.DeletedBy = null;
        team.Status = TeamStatus.Active;
        team.ModifiedOn = DateTime.UtcNow;

        // Reactivate user-team relationships
        var userTeams = await _context.UserTeams
            .IgnoreQueryFilters()
            .Where(ut => ut.TeamId == teamId)
            .ToListAsync();

        foreach (var userTeam in userTeams)
        {
            userTeam.IsActive = true;
            userTeam.ModifiedOn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Recovered team {TeamId}", teamId);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task<GlobalAdminTeamDto> TransferOwnershipAsync(Guid teamId, GlobalAdminTransferOwnershipDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var team = await _context.Teams
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        var newOwner = await _userManager.FindByIdAsync(dto.NewOwnerId.ToString());
        if (newOwner == null)
        {
            throw new InvalidOperationException($"User with ID {dto.NewOwnerId} not found");
        }

        // Check if new owner is already the owner of this team
        if (team.OwnerId == dto.NewOwnerId)
        {
            throw new InvalidOperationException($"User {newOwner.Email} is already the owner of this team");
        }

        // Note: Removed validation that prevented users from owning multiple teams
        // as per requirements: "a user can be a Team Owner of one team, Team Admin of another"

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update team owner
                var oldOwnerId = team.OwnerId;
                team.OwnerId = dto.NewOwnerId;
                team.ModifiedOn = DateTime.UtcNow;
                _context.Teams.Update(team);
                
                // Remove old owner from TeamOwner role
                var oldOwnerTeam = await _context.UserTeams
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(ut => ut.UserId == oldOwnerId && ut.TeamId == teamId);

                if (oldOwnerTeam != null)
                {
                    oldOwnerTeam.Role = TeamRole.TeamMember; // Change to member role
                    oldOwnerTeam.MemberType = MemberType.Coach; // Change to athlete type
                    _context.UserTeams.Update(oldOwnerTeam);
                }

                // Update new owner's role to TeamOwner
                var newOwnerTeam = await _context.UserTeams
                    .FirstOrDefaultAsync(ut => ut.UserId == dto.NewOwnerId && ut.TeamId == teamId);

                if (newOwnerTeam != null)
                {
                    // Update existing relationship
                    newOwnerTeam.Role = TeamRole.TeamOwner;
                    newOwnerTeam.MemberType = MemberType.Coach;
                    newOwnerTeam.IsActive = true;
                    _context.UserTeams.Update(newOwnerTeam);
                }
                else
                {
                    // Create new relationship
                    newOwnerTeam = new UserTeam
                    {
                        UserId = dto.NewOwnerId,
                        TeamId = teamId,
                        Role = TeamRole.TeamOwner,
                        MemberType = MemberType.Coach,
                        IsActive = true,
                        IsDefault = newOwner.DefaultTeamId == null,
                        JoinedOn = DateTime.UtcNow
                    };
                    _context.UserTeams.Add(newOwnerTeam);
                }

                // Update new owner's default team if they don't have one
                if (newOwner.DefaultTeamId == null)
                {
                    newOwner.DefaultTeamId = teamId;
                    await _userManager.UpdateAsync(newOwner);
                }

                // Cancel any pending ownership transfers
                var pendingTransfers = await _context.OwnershipTransfers
                    .Where(ot => ot.TeamId == teamId && ot.Status == OwnershipTransferStatus.Pending)
                    .ToListAsync();

                foreach (var transfer in pendingTransfers)
                {
                    if (transfer.NewOwnerEmail.Equals(newOwner.Email, StringComparison.InvariantCultureIgnoreCase))
                    {
                        transfer.Status = OwnershipTransferStatus.Completed;
                        transfer.CompletedByUserId = _currentUserService.UserId;
                        transfer.CompletedOn = DateTime.UtcNow;
                        transfer.Message = dto.Message;
                        continue;
                    }
                    
                    // Cancel other pending transfers
                    transfer.Status = OwnershipTransferStatus.Cancelled;
                    transfer.Message = "Team ownership transfer cancelled due to new transfer";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Transferred ownership of team {TeamId} from {OldOwnerId} to {NewOwnerId}", 
                    teamId, oldOwnerId, dto.NewOwnerId);

                return await GetTeamByIdAsync(teamId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTeamId = null)
    {
        await _authorizationService.RequireGlobalAdminAsync();
        return await _teamManager.IsSubdomainAvailableAsync(subdomain, excludeTeamId);
    }
} 