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

    public GlobalAdminTeamService(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<GlobalAdminTeamService> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
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
            .Include(t => t.Users.Where(ut => ut.Role == TeamRole.TeamOwner))
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

        // Validate subdomain availability
        if (!await IsSubdomainAvailableAsync(dto.Subdomain))
        {
            throw new InvalidOperationException($"Subdomain '{dto.Subdomain}' is already taken");
        }

        // Check if user with email already exists
        var existingUser = await _userManager.FindByEmailAsync(dto.OwnerEmail);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email '{dto.OwnerEmail}' already exists");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create the new user
            var newUser = new ApplicationUser
            {
                UserName = dto.OwnerEmail,
                Email = dto.OwnerEmail,
                FirstName = dto.OwnerFirstName,
                LastName = dto.OwnerLastName,
                EmailConfirmed = true,
                IsActive = true,
                Status = UserStatus.Active,
                CreatedOn = DateTime.UtcNow
            };

            var userResult = await _userManager.CreateAsync(newUser, dto.OwnerPassword);
            if (!userResult.Succeeded)
            {
                var errors = string.Join(", ", userResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            // Create the team
            var team = new Team
            {
                Name = dto.Name,
                Subdomain = dto.Subdomain,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                Status = TeamStatus.Active,
                Tier = dto.Tier,
                ExpiresOn = dto.ExpiresOn,
                OwnerId = newUser.Id,
                CreatedOn = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Create the owner relationship in UserTeam
            var userTeam = new UserTeam
            {
                UserId = newUser.Id,
                TeamId = team.Id,
                Role = TeamRole.TeamOwner,
                MemberType = MemberType.Coach, // Default for team owners
                IsActive = true,
                IsDefault = true,
                JoinedOn = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            _context.UserTeams.Add(userTeam);

            // Update user's default team
            newUser.DefaultTeamId = team.Id;
            await _userManager.UpdateAsync(newUser);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Created team {TeamId} with new owner {UserId}", team.Id, newUser.Id);

            return await GetTeamByIdAsync(team.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<GlobalAdminTeamDto> CreateTeamWithExistingOwnerAsync(CreateTeamWithExistingOwnerDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        // Validate subdomain availability
        if (!await IsSubdomainAvailableAsync(dto.Subdomain))
        {
            throw new InvalidOperationException($"Subdomain '{dto.Subdomain}' is already taken");
        }

        // Validate that the user exists
        var owner = await _userManager.FindByIdAsync(dto.OwnerId.ToString());
        if (owner == null)
        {
            throw new InvalidOperationException($"User with ID {dto.OwnerId} not found");
        }

        // Check if user is already an owner of another team
        var existingOwnership = await _context.UserTeams
            .AnyAsync(ut => ut.UserId == dto.OwnerId && ut.Role == TeamRole.TeamOwner && ut.IsActive);
        
        if (existingOwnership)
        {
            throw new InvalidOperationException($"User {owner.Email} is already the owner of another team");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create the team
            var team = new Team
            {
                Name = dto.Name,
                Subdomain = dto.Subdomain,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                Status = TeamStatus.Active,
                Tier = dto.Tier,
                ExpiresOn = dto.ExpiresOn,
                OwnerId = dto.OwnerId,
                CreatedOn = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Create or update the owner relationship in UserTeam
            var existingUserTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.UserId == dto.OwnerId && ut.TeamId == team.Id);

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
                    UserId = dto.OwnerId,
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

            _logger.LogInformation("Created team {TeamId} with existing owner {UserId}", team.Id, dto.OwnerId);

            return await GetTeamByIdAsync(team.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

    public async Task PermanentlyDeleteTeamAsync(Guid teamId)
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove all user-team relationships
            _context.UserTeams.RemoveRange(team.Users);

            // Remove ownership transfers
            var ownershipTransfers = await _context.OwnershipTransfers
                .Where(ot => ot.TeamId == teamId)
                .ToListAsync();
            _context.OwnershipTransfers.RemoveRange(ownershipTransfers);

            // Remove the team
            _context.Teams.Remove(team);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Permanently deleted team {TeamId}", teamId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

        // Check if new owner is already an owner of another team
        var existingOwnership = await _context.UserTeams
            .AnyAsync(ut => ut.UserId == dto.NewOwnerId && ut.Role == TeamRole.TeamOwner && ut.IsActive);
        
        if (existingOwnership)
        {
            throw new InvalidOperationException($"User {newOwner.Email} is already the owner of another team");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update team owner
            var oldOwnerId = team.OwnerId;
            team.OwnerId = dto.NewOwnerId;
            team.ModifiedOn = DateTime.UtcNow;

            // Update old owner's role to TeamAdmin
            var oldOwnerTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.UserId == oldOwnerId && ut.TeamId == teamId);
            
            if (oldOwnerTeam != null)
            {
                oldOwnerTeam.Role = TeamRole.TeamAdmin;
                oldOwnerTeam.ModifiedOn = DateTime.UtcNow;
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
                newOwnerTeam.ModifiedOn = DateTime.UtcNow;
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
                    JoinedOn = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow
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
                transfer.Status = OwnershipTransferStatus.Cancelled;
                transfer.ModifiedOn = DateTime.UtcNow;
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
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTeamId = null)
    {
        var query = _context.Teams
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.Subdomain.ToLower() == subdomain.ToLower());

        if (excludeTeamId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTeamId.Value);
        }

        return !await query.AnyAsync();
    }
} 