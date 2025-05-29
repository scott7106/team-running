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
using TeamStride.Infrastructure.Data.Extensions;

namespace TeamStride.Infrastructure.Services;

/// <summary>
/// Implementation of team management service for standard users.
/// Operations are scoped to teams the user has access to based on their roles.
/// </summary>
public class StandardTeamService : IStandardTeamService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<StandardTeamService> _logger;

    private IQueryable<Team> DbTeams => _context.Teams
        .WhereIf(_currentUserService.CurrentTeamId.HasValue,
            e => e.Users.Any(x => x.TeamId == _currentUserService.CurrentTeamId!.Value));
    
    private IQueryable<Team> DbTeamsWithDetails => DbTeams
        .Include(t => t.Users)
        .ThenInclude(ut => ut.User);

    public StandardTeamService(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<StandardTeamService> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedList<TeamDto>> GetTeamsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null,
        TeamStatus? status = null,
        TeamTier? tier = null)
    {
        // For standard users, only return teams they have access to
        // Global admins can see all teams through the GlobalAdminTeamService
        
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        IQueryable<Team> query = DbTeamsWithDetails;

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.ToLower();
            query = query.Where(t => 
                t.Name.ToLower().Contains(search) ||
                t.Subdomain.ToLower().Contains(search));
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

        // Order by name
        query = query.OrderBy(t => t.Name);

        var totalCount = await query.CountAsync();
        var teams = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var teamDtos = new List<TeamDto>();
        foreach (var team in teams)
        {
            var dto = await MapToTeamDtoAsync(team);
            teamDtos.Add(dto);
        }

        return new PaginatedList<TeamDto>(teamDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<TeamDto> GetTeamByIdAsync(Guid teamId)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId);

        var team = await DbTeamsWithDetails.FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        return await MapToTeamDtoAsync(team);
    }

    public async Task<TeamDto> GetTeamBySubdomainAsync(string subdomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain);

        // explicity chose not to use DbQuery here
        var team = await DbTeamsWithDetails
            .IgnoreQueryFilters() // Allow subdomain lookup without team context
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && !t.IsDeleted);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with subdomain '{subdomain}' not found");
        }

        // Check if user has access to this team (if authenticated)
        if (_currentUserService.IsAuthenticated && !_currentUserService.IsGlobalAdmin)
        {
            var hasAccess = await _context.UserTeams
                .AnyAsync(ut => ut.UserId == _currentUserService.UserId && 
                               ut.TeamId == team.Id && 
                               ut.IsActive);

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException($"Access denied to team with subdomain '{subdomain}'");
            }
        }

        return await MapToTeamDtoAsync(team);
    }

    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Only global admins can create teams through this service
        // Regular team creation should go through registration flow
        await _authorizationService.RequireGlobalAdminAsync();

        // Validate subdomain availability
        if (!await IsSubdomainAvailableAsync(dto.Subdomain))
        {
            throw new InvalidOperationException($"Subdomain '{dto.Subdomain}' is already taken");
        }

        // Check if owner email exists
        var owner = await _userManager.FindByEmailAsync(dto.OwnerEmail);
        if (owner == null)
        {
            throw new InvalidOperationException($"User with email '{dto.OwnerEmail}' not found. Use CreateTeamWithNewOwner for new users.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Subdomain = dto.Subdomain.ToLower(),
                OwnerId = owner.Id,
                Tier = dto.Tier,
                Status = TeamStatus.Active,
                PrimaryColor = dto.PrimaryColor,
                SecondaryColor = dto.SecondaryColor,
                ExpiresOn = dto.Tier == TeamTier.Free ? null : DateTime.UtcNow.AddYears(1),
                CreatedOn = DateTime.UtcNow
            };

            _context.Teams.Add(team);

            // Create or update the owner relationship
            var existingUserTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.UserId == owner.Id && ut.TeamId == team.Id);

            if (existingUserTeam != null)
            {
                existingUserTeam.Role = TeamRole.TeamOwner;
                existingUserTeam.MemberType = MemberType.Coach;
                existingUserTeam.IsActive = true;
                existingUserTeam.ModifiedOn = DateTime.UtcNow;
            }
            else
            {
                var userTeam = new UserTeam
                {
                    UserId = owner.Id,
                    TeamId = team.Id,
                    Role = TeamRole.TeamOwner,
                    MemberType = MemberType.Coach,
                    IsActive = true,
                    IsDefault = owner.DefaultTeamId == null,
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

            _logger.LogInformation("Created team {TeamId} with owner {UserId}", team.Id, owner.Id);

            return await GetTeamByIdAsync(team.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TeamDto> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var team = await DbTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Update properties
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            team.Name = dto.Name;
        }

        if (dto.Status.HasValue)
        {
            team.Status = dto.Status.Value;
        }

        if (dto.ExpiresOn.HasValue)
        {
            team.ExpiresOn = dto.ExpiresOn.Value;
        }

        team.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated team {TeamId}", teamId);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task DeleteTeamAsync(Guid teamId)
    {
        await _authorizationService.RequireTeamOwnershipAsync(teamId);

        var team = await DbTeams.FirstOrDefaultAsync(t => t.Id == teamId);
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

    public async Task<OwnershipTransferDto> InitiateOwnershipTransferAsync(Guid teamId, InitiateOwnershipTransferDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _authorizationService.RequireTeamOwnershipAsync(teamId);

        var team = await DbTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Check if there's already a pending transfer
        var existingTransfer = await _context.OwnershipTransfers
            .FirstOrDefaultAsync(ot => ot.TeamId == teamId && ot.Status == OwnershipTransferStatus.Pending);

        if (existingTransfer != null)
        {
            throw new InvalidOperationException("There is already a pending ownership transfer for this team");
        }

        // Validate new owner
        ApplicationUser? newOwner = null;
        if (dto.ExistingMemberId.HasValue)
        {
            // Transfer to existing team member
            var existingMember = await _context.UserTeams
                .Include(ut => ut.User)
                .FirstOrDefaultAsync(ut => ut.UserId == dto.ExistingMemberId.Value && 
                                          ut.TeamId == teamId && 
                                          ut.IsActive);

            if (existingMember == null)
            {
                throw new InvalidOperationException("Specified user is not an active member of this team");
            }

            newOwner = existingMember.User;
        }
        else
        {
            // Transfer to user by email (may or may not exist)
            newOwner = await _userManager.FindByEmailAsync(dto.NewOwnerEmail);
        }

        // Check if new owner is already an owner of another team
        if (newOwner != null)
        {
            // Note: Removed validation that prevented users from owning multiple teams
            // as per requirements: "a user can be a Team Owner of one team, Team Admin of another"
        }

        var transfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InitiatedByUserId = _currentUserService.UserId!.Value,
            NewOwnerEmail = dto.NewOwnerEmail,
            NewOwnerFirstName = dto.NewOwnerFirstName,
            NewOwnerLastName = dto.NewOwnerLastName,
            ExistingMemberId = dto.ExistingMemberId,
            Message = dto.Message,
            ExpiresOn = DateTime.UtcNow.AddDays(7), // 7 days to complete transfer
            Status = OwnershipTransferStatus.Pending,
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow
        };

        _context.OwnershipTransfers.Add(transfer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Initiated ownership transfer {TransferId} for team {TeamId} to {NewOwnerEmail}", 
            transfer.Id, teamId, dto.NewOwnerEmail);

        return GetOwnershipTransferDto(transfer);
    }

    public async Task<TeamDto> CompleteOwnershipTransferAsync(string transferToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transferToken);

        var transfer = await _context.OwnershipTransfers
            .IgnoreQueryFilters()
            .Include(ot => ot.Team)
            .Include(ot => ot.InitiatedByUser)
            .FirstOrDefaultAsync(ot => ot.TransferToken == transferToken);

        if (transfer == null)
        {
            throw new InvalidOperationException("Invalid transfer token");
        }

        if (transfer.Status != OwnershipTransferStatus.Pending)
        {
            throw new InvalidOperationException($"Transfer is {transfer.Status.ToString().ToLower()} and cannot be completed");
        }

        if (transfer.ExpiresOn < DateTime.UtcNow)
        {
            transfer.Status = OwnershipTransferStatus.Expired;
            transfer.ModifiedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Transfer token has expired");
        }

        // Find or create the new owner
        var newOwner = await _userManager.FindByEmailAsync(transfer.NewOwnerEmail);
        if (newOwner == null)
        {
            throw new InvalidOperationException("New owner user account not found. Please register first.");
        }

        // Verify the current user is the intended new owner
        if (_currentUserService.UserId != newOwner.Id)
        {
            throw new UnauthorizedAccessException("Only the intended new owner can complete this transfer");
        }

        // Note: Removed validation that prevented users from owning multiple teams
        // as per requirements: "a user can be a Team Owner of one team, Team Admin of another"

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update team owner
            var team = transfer.Team;
            var oldOwnerId = team.OwnerId;
            team.OwnerId = newOwner.Id;
            team.ModifiedOn = DateTime.UtcNow;

            // Update old owner's role to TeamAdmin
            var oldOwnerTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.UserId == oldOwnerId && ut.TeamId == team.Id);

            if (oldOwnerTeam != null)
            {
                oldOwnerTeam.Role = TeamRole.TeamAdmin;
                oldOwnerTeam.ModifiedOn = DateTime.UtcNow;
            }

            // Update new owner's role to TeamOwner
            var newOwnerTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.UserId == newOwner.Id && ut.TeamId == team.Id);

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
                    UserId = newOwner.Id,
                    TeamId = team.Id,
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
                newOwner.DefaultTeamId = team.Id;
                await _userManager.UpdateAsync(newOwner);
            }

            // Complete the transfer
            transfer.Status = OwnershipTransferStatus.Completed;
            transfer.CompletedOn = DateTime.UtcNow;
            transfer.CompletedByUserId = newOwner.Id;
            transfer.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Completed ownership transfer {TransferId} for team {TeamId} from {OldOwnerId} to {NewOwnerId}", 
                transfer.Id, team.Id, oldOwnerId, newOwner.Id);

            return await GetTeamByIdAsync(team.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task CancelOwnershipTransferAsync(Guid transferId)
    {
        var transfer = await _context.OwnershipTransfers
            .FirstOrDefaultAsync(ot => ot.Id == transferId);

        if (transfer == null)
        {
            throw new InvalidOperationException($"Transfer with ID {transferId} not found");
        }

        await _authorizationService.RequireTeamOwnershipAsync(transfer.TeamId);

        if (transfer.Status != OwnershipTransferStatus.Pending)
        {
            throw new InvalidOperationException($"Transfer is {transfer.Status.ToString().ToLower()} and cannot be cancelled");
        }

        transfer.Status = OwnershipTransferStatus.Cancelled;
        transfer.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled ownership transfer {TransferId} for team {TeamId}", 
            transferId, transfer.TeamId);
    }

    public async Task<IEnumerable<OwnershipTransferDto>> GetPendingTransfersAsync(Guid teamId)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var transfers = await _context.OwnershipTransfers
            .Include(ot => ot.Team)
            .Include(ot => ot.InitiatedByUser)
            .Where(ot => ot.TeamId == teamId && ot.Status == OwnershipTransferStatus.Pending)
            .OrderByDescending(ot => ot.CreatedOn)
            .ToListAsync();

        var transferDtos = new List<OwnershipTransferDto>();
        foreach (var transfer in transfers)
        {
            transferDtos.Add(GetOwnershipTransferDto(transfer));
        }

        return transferDtos;
    }

    public async Task<TeamDto> UpdateSubscriptionAsync(Guid teamId, UpdateSubscriptionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _authorizationService.RequireTeamOwnershipAsync(teamId);

        var team = await DbTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Validate tier change
        if (dto.NewTier < team.Tier)
        {
            // Check if downgrade is allowed (e.g., athlete count within new tier limits)
            var tierLimits = GetTierLimits(dto.NewTier);
            var athleteCount = await _context.UserTeams
                .CountAsync(ut => ut.TeamId == teamId && 
                                 ut.IsActive && 
                                 ut.MemberType == MemberType.Athlete);

            if (athleteCount > tierLimits.MaxAthletes)
            {
                throw new InvalidOperationException($"Cannot downgrade to {dto.NewTier} tier. Current athlete count ({athleteCount}) exceeds the limit ({tierLimits.MaxAthletes})");
            }
        }

        team.Tier = dto.NewTier;
        if (dto.ExpiresOn.HasValue)
        {
            team.ExpiresOn = dto.ExpiresOn.Value;
        }
        team.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated subscription for team {TeamId} to {NewTier}", teamId, dto.NewTier);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task<TeamDto> UpdateBrandingAsync(Guid teamId, UpdateTeamBrandingDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var team = await DbTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Check if team tier allows custom branding
        var tierLimits = GetTierLimits(team.Tier);
        if (!tierLimits.AllowCustomBranding)
        {
            throw new InvalidOperationException($"Custom branding is not available for {team.Tier} tier");
        }

        // Update branding properties
        if (!string.IsNullOrWhiteSpace(dto.PrimaryColor))
        {
            team.PrimaryColor = dto.PrimaryColor;
        }

        if (!string.IsNullOrWhiteSpace(dto.SecondaryColor))
        {
            team.SecondaryColor = dto.SecondaryColor;
        }

        if (!string.IsNullOrWhiteSpace(dto.LogoUrl))
        {
            team.LogoUrl = dto.LogoUrl;
        }

        team.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated branding for team {TeamId}", teamId);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task<PaginatedList<TeamMemberDto>> GetTeamMembersAsync(
        Guid teamId,
        int pageNumber = 1,
        int pageSize = 10,
        TeamRole? role = null)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId);

        var query = _context.UserTeams
            .Include(ut => ut.User)
            .Where(ut => ut.TeamId == teamId && ut.IsActive);

        // Apply role filter
        if (role.HasValue)
        {
            query = query.Where(ut => ut.Role == role.Value);
        }

        // Order by role (owners first, then admins, then members) and then by name
        query = query.OrderBy(ut => ut.Role)
                    .ThenBy(ut => ut.User.LastName)
                    .ThenBy(ut => ut.User.FirstName);

        var totalCount = await query.CountAsync();
        var userTeams = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var memberDtos = userTeams.Select(ut => _mapper.Map<TeamMemberDto>(ut)).ToList();

        return new PaginatedList<TeamMemberDto>(memberDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<TeamMemberDto> UpdateMemberRoleAsync(Guid teamId, Guid userId, TeamRole newRole)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var userTeam = await _context.UserTeams
            .Include(ut => ut.User)
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId);

        if (userTeam == null)
        {
            throw new InvalidOperationException($"User {userId} is not a member of team {teamId}");
        }

        // Prevent changing the owner's role
        if (userTeam.Role == TeamRole.TeamOwner)
        {
            throw new InvalidOperationException("Cannot change the role of the team owner. Use ownership transfer instead.");
        }

        // Prevent promoting to owner
        if (newRole == TeamRole.TeamOwner)
        {
            throw new InvalidOperationException("Cannot promote user to owner. Use ownership transfer instead.");
        }

        userTeam.Role = newRole;
        userTeam.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated role for user {UserId} in team {TeamId} to {NewRole}", 
            userId, teamId, newRole);

        return _mapper.Map<TeamMemberDto>(userTeam);
    }

    public async Task RemoveMemberAsync(Guid teamId, Guid userId)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId);

        if (userTeam == null)
        {
            throw new InvalidOperationException($"User {userId} is not a member of team {teamId}");
        }

        // Prevent removing the owner
        if (userTeam.Role == TeamRole.TeamOwner)
        {
            throw new InvalidOperationException("Cannot remove the team owner. Use ownership transfer first.");
        }

        userTeam.IsActive = false;
        userTeam.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed user {UserId} from team {TeamId}", userId, teamId);
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain);

        var normalizedSubdomain = subdomain.ToLower();
        // explicity chose not to use DbQuery here
        var exists = await _context.Teams
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Subdomain == normalizedSubdomain && !t.IsDeleted);

        return !exists;
    }

    public async Task<TeamDto> UpdateSubdomainAsync(Guid teamId, string newSubdomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newSubdomain);
        await _authorizationService.RequireTeamOwnershipAsync(teamId);

        var team = await DbTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        var normalizedSubdomain = newSubdomain.ToLower();
        if (team.Subdomain == normalizedSubdomain)
        {
            return await GetTeamByIdAsync(teamId); // No change needed
        }

        if (!await IsSubdomainAvailableAsync(normalizedSubdomain))
        {
            throw new InvalidOperationException($"Subdomain '{normalizedSubdomain}' is already taken");
        }

        team.Subdomain = normalizedSubdomain;
        team.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated subdomain for team {TeamId} to {NewSubdomain}", teamId, normalizedSubdomain);

        return await GetTeamByIdAsync(teamId);
    }

    public Task<TeamTierLimitsDto> GetTierLimitsAsync(TeamTier tier)
    {
        var limits = GetTierLimits(tier);
        return Task.FromResult(limits);
    }

    public async Task<bool> CanAddAthleteAsync(Guid teamId)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId);

        var team = await DbTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null)
        {
            return false;
        }

        var tierLimits = GetTierLimits(team.Tier);
        var currentAthleteCount = await _context.UserTeams
            .CountAsync(ut => ut.TeamId == teamId && 
                             ut.IsActive && 
                             ut.MemberType == MemberType.Athlete);

        return currentAthleteCount < tierLimits.MaxAthletes;
    }

    private async Task<TeamDto> MapToTeamDtoAsync(Team team)
    {
        var dto = _mapper.Map<TeamDto>(team);

        // Get owner information - every team must have exactly one owner
        var owner = team.Users.FirstOrDefault(ut => ut.Role == TeamRole.TeamOwner);
        if (owner?.User == null)
        {
            throw new InvalidOperationException($"Team {team.Id} does not have a valid team owner. This is a data integrity issue.");
        }
        
        dto.Owner = _mapper.Map<TeamMemberDto>(owner);

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

    private OwnershipTransferDto GetOwnershipTransferDto(OwnershipTransfer transfer)
    {
        var dto = _mapper.Map<OwnershipTransferDto>(transfer);
        return dto;
    }

    private static TeamTierLimitsDto GetTierLimits(TeamTier tier)
    {
        return tier switch
        {
            TeamTier.Free => new TeamTierLimitsDto
            {
                Tier = TeamTier.Free,
                MaxAthletes = 7,
                MaxAdmins = 2,
                MaxCoaches = 2,
                AllowCustomBranding = false,
                AllowAdvancedReporting = false
            },
            TeamTier.Standard => new TeamTierLimitsDto
            {
                Tier = TeamTier.Standard,
                MaxAthletes = 30,
                MaxAdmins = 5,
                MaxCoaches = 5,
                AllowCustomBranding = false,
                AllowAdvancedReporting = false
            },
            TeamTier.Premium => new TeamTierLimitsDto
            {
                Tier = TeamTier.Premium,
                MaxAthletes = int.MaxValue,
                MaxAdmins = int.MaxValue,
                MaxCoaches = int.MaxValue,
                AllowCustomBranding = true,
                AllowAdvancedReporting = true
            },
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };
    }
} 