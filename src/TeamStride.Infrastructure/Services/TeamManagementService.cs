using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Email;

namespace TeamStride.Infrastructure.Services;

public partial class TeamManagementService : ITeamManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly ILogger<TeamManagementService> _logger;

    // Subdomain blacklist
    private static readonly HashSet<string> BlacklistedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "host", "mail", "ftp", "smtp", "pop", "imap", "email",
        "support", "help", "blog", "news", "app", "mobile", "web", "secure", "static",
        "ssl", "tls", "cdn", "static", "assets", "media", "images", "files", "status",
        "download", "upload", "backup", "test", "dev", "staging", "prod", "control",
        "production", "demo", "beta", "alpha", "preview", "temp", "tmp", "manage",
        "root", "system", "config", "settings", "dashboard", "panel",
        "control", "manage", "manager", "teamstride", "team-stride", 
        "shit", "fuck", "damn", "asshole", "bitch", "cunt", "dick", "faggot",
        "nigga", "nigger", "pussy", "whore", "slut", "douche", "douchebag",
        "douchecanoe", "douchewaffle", "douchelord", "douchebaglord", "shitlord",
        "ass", "fag", "faggot", "faggotlord", "niglet", "negro", "spic", "spick"
    };

    public TeamManagementService(
        ApplicationDbContext context,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        ILogger<TeamManagementService> logger)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PaginatedList<TeamManagementDto>> GetTeamsAsync(
        int pageNumber = 1, 
        int pageSize = 10, 
        string? searchQuery = null,
        TeamStatus? status = null,
        TeamTier? tier = null)
    {
        //TODO: Team admins should only see their own teams, global admins should see all teams
        await EnsureGlobalAdminAsync();

        var query = _context.Teams
            .IgnoreQueryFilters() // Global admins can see all teams
            .Include(t => t.Users.Where(ut => ut.Role == TeamRole.Host))
                .ThenInclude(ut => ut.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(t => t.Name.Contains(searchQuery) || t.Subdomain.Contains(searchQuery));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (tier.HasValue)
        {
            query = query.Where(t => t.Tier == tier.Value);
        }

        // Order by team name
        query = query.OrderBy(t => t.Name);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var teams = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TeamManagementDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                LogoUrl = t.LogoUrl,
                PrimaryColor = t.PrimaryColor,
                SecondaryColor = t.SecondaryColor,
                Status = t.Status,
                Tier = t.Tier,
                ExpiresOn = t.ExpiresOn,
                CreatedOn = t.CreatedOn,
                ModifiedOn = t.ModifiedOn,
                Owner = t.Users.Where(ut => ut.Role == TeamRole.Host).Select(ut => new TeamMemberDto
                {
                    Id = ut.Id,
                    UserId = ut.UserId,
                    Email = ut.User.Email!,
                    FirstName = ut.User.FirstName,
                    LastName = ut.User.LastName,
                    DisplayName = $"{ut.User.LastName}, {ut.User.FirstName}",
                    Role = ut.Role,
                    JoinedOn = ut.JoinedOn,
                    IsActive = ut.IsActive,
                    IsOwner = true
                }).FirstOrDefault(),
                MemberCount = t.Users.Count(ut => ut.IsActive),
                AthleteCount = t.Users.Count(ut => ut.IsActive && ut.Role == TeamRole.Athlete),
                AdminCount = t.Users.Count(ut => ut.IsActive && (ut.Role == TeamRole.Admin || ut.Role == TeamRole.Host)),
                HasPendingOwnershipTransfer = _context.OwnershipTransfers.Any(ot => ot.TeamId == t.Id && ot.Status == OwnershipTransferStatus.Pending)
            })
            .ToListAsync();

        return new PaginatedList<TeamManagementDto>(teams, totalCount, pageNumber, pageSize);
    }

    public async Task<TeamManagementDto> GetTeamByIdAsync(Guid teamId)
    {
        await EnsureCanAccessTeamAsync(teamId);

        var team = await GetTeamWithDetailsAsync(teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        return team;
    }

    public async Task<TeamManagementDto> GetTeamBySubdomainAsync(string subdomain)
    {
        var team = await _context.Teams
            .IgnoreQueryFilters()
            .Include(t => t.Users.Where(ut => ut.Role == TeamRole.Host))
                .ThenInclude(ut => ut.User)
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain);

        if (team == null)
        {
            throw new InvalidOperationException($"Team with subdomain '{subdomain}' not found");
        }

        await EnsureCanAccessTeamAsync(team.Id);

        return await GetTeamWithDetailsAsync(team.Id) ?? 
            throw new InvalidOperationException($"Team with subdomain '{subdomain}' not found");
    }

    // Ownership transfer methods
    public async Task<OwnershipTransferDto> InitiateOwnershipTransferAsync(Guid teamId, TransferOwnershipDto dto)
    {
        await EnsureCanTransferOwnershipAsync(teamId);

        var team = await _context.Teams.FindAsync(teamId);
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

        // Validate existing member if specified
        ApplicationUser? existingMember = null;
        if (dto.ExistingMemberId.HasValue)
        {
            var userTeam = await _context.UserTeams
                .Include(ut => ut.User)
                .FirstOrDefaultAsync(ut => ut.UserId == dto.ExistingMemberId.Value && ut.TeamId == teamId && ut.IsActive);

            if (userTeam == null)
            {
                throw new InvalidOperationException("Specified user is not an active member of this team");
            }

            existingMember = userTeam.User;
            
            // Use existing member's details if not provided
            if (string.IsNullOrWhiteSpace(dto.NewOwnerFirstName))
                dto.NewOwnerFirstName = existingMember.FirstName;
            if (string.IsNullOrWhiteSpace(dto.NewOwnerLastName))
                dto.NewOwnerLastName = existingMember.LastName;
        }

        // Create the transfer
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
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            Status = OwnershipTransferStatus.Pending,
            TransferToken = GenerateSecureToken()
        };

        _context.OwnershipTransfers.Add(transfer);
        await _context.SaveChangesAsync();

        // Send email
        var initiatedByUser = await _userManager.FindByIdAsync(_currentUserService.UserId!.Value.ToString());
        var initiatedByName = $"{initiatedByUser!.FirstName} {initiatedByUser.LastName}";
        var transferLink = $"https://teamstride.com/transfer/complete?token={transfer.TransferToken}";

        await _emailService.SendOwnershipTransferAsync(
            dto.NewOwnerEmail, 
            team.Name, 
            initiatedByName, 
            transferLink, 
            dto.Message);

        _logger.LogInformation("Ownership transfer initiated for team {TeamId} to {Email} by user {UserId}", 
            teamId, dto.NewOwnerEmail, _currentUserService.UserId);

        return _mapper.Map<OwnershipTransferDto>(transfer);
    }

    public async Task<TeamManagementDto> CompleteOwnershipTransferAsync(string transferToken)
    {
        var transfer = await _context.OwnershipTransfers
            .Include(ot => ot.Team)
            .Include(ot => ot.InitiatedByUser)
            .FirstOrDefaultAsync(ot => ot.TransferToken == transferToken);

        if (transfer == null)
        {
            throw new InvalidOperationException("Invalid transfer token");
        }

        if (transfer.Status != OwnershipTransferStatus.Pending)
        {
            throw new InvalidOperationException("Transfer is no longer pending");
        }

        if (transfer.ExpiresOn < DateTime.UtcNow)
        {
            transfer.Status = OwnershipTransferStatus.Expired;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Transfer token has expired");
        }

        // Find or create the new owner
        var newOwner = await _userManager.FindByEmailAsync(transfer.NewOwnerEmail);
        var isNewUser = newOwner == null;

        if (isNewUser)
        {
            newOwner = new ApplicationUser
            {
                UserName = transfer.NewOwnerEmail,
                Email = transfer.NewOwnerEmail,
                FirstName = transfer.NewOwnerFirstName ?? "Team",
                LastName = transfer.NewOwnerLastName ?? "Owner",
                EmailConfirmed = false,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(newOwner);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create new owner user: {errors}");
            }
        }

        // Update existing owner to Admin role
        var currentOwner = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == transfer.TeamId && ut.Role == TeamRole.Host);

        if (currentOwner != null)
        {
            currentOwner.Role = TeamRole.Admin;
            currentOwner.IsDefault = false;
        }

        // Create or update new owner relationship
        var newOwnerTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == newOwner.Id && ut.TeamId == transfer.TeamId);

        if (newOwnerTeam != null)
        {
            // Update existing relationship
            newOwnerTeam.Role = TeamRole.Host;
            newOwnerTeam.IsDefault = true;
            newOwnerTeam.IsActive = true;
        }
        else
        {
            // Create new relationship
            newOwnerTeam = new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = newOwner.Id,
                TeamId = transfer.TeamId,
                Role = TeamRole.Host,
                IsDefault = true,
                IsActive = true,
                JoinedOn = DateTime.UtcNow
            };
            _context.UserTeams.Add(newOwnerTeam);
        }

        // Complete the transfer
        transfer.Status = OwnershipTransferStatus.Completed;
        transfer.CompletedOn = DateTime.UtcNow;
        transfer.CompletedByUserId = newOwner.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ownership transfer completed for team {TeamId} to user {UserId}", 
            transfer.TeamId, newOwner.Id);

        return await GetTeamByIdAsync(transfer.TeamId);
    }

    public async Task CancelOwnershipTransferAsync(Guid transferId)
    {
        var transfer = await _context.OwnershipTransfers.FindAsync(transferId);
        if (transfer == null)
        {
            throw new InvalidOperationException($"Transfer with ID {transferId} not found");
        }

        await EnsureCanCancelTransferAsync(transfer);

        if (transfer.Status != OwnershipTransferStatus.Pending)
        {
            throw new InvalidOperationException("Transfer is not pending and cannot be cancelled");
        }

        transfer.Status = OwnershipTransferStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ownership transfer {TransferId} cancelled by user {UserId}", 
            transferId, _currentUserService.UserId);
    }

    public async Task<IEnumerable<OwnershipTransferDto>> GetPendingTransfersAsync(Guid teamId)
    {
        await EnsureCanAccessTeamAsync(teamId);

        var transfers = await _context.OwnershipTransfers
            .Include(ot => ot.Team)
            .Include(ot => ot.InitiatedByUser)
            .Where(ot => ot.TeamId == teamId && ot.Status == OwnershipTransferStatus.Pending)
            .OrderByDescending(ot => ot.CreatedOn)
            .ToListAsync();

        return _mapper.Map<IEnumerable<OwnershipTransferDto>>(transfers);
    }

    // Subscription and branding methods
    public async Task<TeamManagementDto> UpdateSubscriptionAsync(Guid teamId, UpdateSubscriptionDto dto)
    {
        await EnsureCanManageSubscriptionAsync(teamId);

        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Validate tier downgrade doesn't exceed limits
        if (dto.NewTier < team.Tier)
        {
            var canDowngrade = await CanDowngradeToTierAsync(teamId, dto.NewTier);
            if (!canDowngrade)
            {
                var limits = await GetTierLimitsAsync(dto.NewTier);
                throw new InvalidOperationException($"Cannot downgrade to {dto.NewTier} tier. Current team exceeds the limit of {limits.MaxAthletes} athletes.");
            }
        }

        team.Tier = dto.NewTier;
        team.ExpiresOn = dto.ExpiresOn;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} subscription updated to {Tier} by user {UserId}", 
            teamId, dto.NewTier, _currentUserService.UserId);

        return await GetTeamByIdAsync(teamId);
    }

    public async Task<TeamManagementDto> UpdateBrandingAsync(Guid teamId, UpdateTeamBrandingDto dto)
    {
        await EnsureCanUpdateBrandingAsync(teamId);

        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {teamId} not found");
        }

        // Update branding fields if provided
        if (!string.IsNullOrWhiteSpace(dto.PrimaryColor))
        {
            team.PrimaryColor = dto.PrimaryColor;
        }

        if (!string.IsNullOrWhiteSpace(dto.SecondaryColor))
        {
            team.SecondaryColor = dto.SecondaryColor;
        }

        if (dto.LogoUrl != null)
        {
            team.LogoUrl = dto.LogoUrl;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} branding updated by user {UserId}", teamId, _currentUserService.UserId);

        return await GetTeamByIdAsync(teamId);
    }

    // Member management methods
    public async Task<PaginatedList<TeamMemberDto>> GetTeamMembersAsync(
        Guid teamId, 
        int pageNumber = 1, 
        int pageSize = 10,
        TeamRole? role = null)
    {
        await EnsureCanAccessTeamAsync(teamId);

        var query = _context.UserTeams
            .Include(ut => ut.User)
            .Where(ut => ut.TeamId == teamId && ut.IsActive);

        if (role.HasValue)
        {
            query = query.Where(ut => ut.Role == role.Value);
        }

        query = query.OrderBy(ut => ut.User.LastName).ThenBy(ut => ut.User.FirstName);

        var members = await query
            .Select(ut => new TeamMemberDto
            {
                Id = ut.Id,
                UserId = ut.UserId,
                Email = ut.User.Email!,
                FirstName = ut.User.FirstName,
                LastName = ut.User.LastName,
                DisplayName = $"{ut.User.LastName}, {ut.User.FirstName}",
                Role = ut.Role,
                JoinedOn = ut.JoinedOn,
                IsActive = ut.IsActive,
                IsOwner = ut.Role == TeamRole.Host
            })
            .ToListAsync();

        return new PaginatedList<TeamMemberDto>(members, members.Count, pageNumber, pageSize);
    }

    public async Task<TeamMemberDto> UpdateMemberRoleAsync(Guid teamId, Guid userId, TeamRole newRole)
    {
        await EnsureCanManageMembersAsync(teamId);

        var userTeam = await _context.UserTeams
            .Include(ut => ut.User)
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId && ut.IsActive);

        if (userTeam == null)
        {
            throw new InvalidOperationException("User is not an active member of this team");
        }

        if (userTeam.Role == TeamRole.Host)
        {
            throw new InvalidOperationException("Cannot change the role of the team owner. Transfer ownership first.");
        }

        if (newRole == TeamRole.Host)
        {
            throw new InvalidOperationException("Cannot assign Host role directly. Use ownership transfer instead.");
        }

        userTeam.Role = newRole;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} role updated to {Role} in team {TeamId} by user {CurrentUserId}", 
            userId, newRole, teamId, _currentUserService.UserId);

        return _mapper.Map<TeamMemberDto>(userTeam);
    }

    public async Task RemoveMemberAsync(Guid teamId, Guid userId)
    {
        await EnsureCanManageMembersAsync(teamId);

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId && ut.IsActive);

        if (userTeam == null)
        {
            throw new InvalidOperationException("User is not an active member of this team");
        }

        if (userTeam.Role == TeamRole.Host)
        {
            throw new InvalidOperationException("Cannot remove the team owner. Transfer ownership first.");
        }

        userTeam.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} removed from team {TeamId} by user {CurrentUserId}", 
            userId, teamId, _currentUserService.UserId);
    }

    // Authorization helper methods
    private async Task EnsureGlobalAdminAsync()
    {
        if (!_currentUserService.IsGlobalAdmin)
        {
            throw new UnauthorizedAccessException("Global admin access required");
        }
    }

    private async Task EnsureCanAccessTeamAsync(Guid teamId)
    {
        if (_currentUserService.IsGlobalAdmin)
            return;

        var hasAccess = await _context.UserTeams
            .AnyAsync(ut => ut.TeamId == teamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access to this team is not authorized");
        }
    }

    private async Task<TeamManagementDto?> GetTeamWithDetailsAsync(Guid teamId)
    {
        return await _context.Teams
            .IgnoreQueryFilters()
            .Include(t => t.Users.Where(ut => ut.Role == TeamRole.Host))
                .ThenInclude(ut => ut.User)
            .Where(t => t.Id == teamId)
            .Select(t => new TeamManagementDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                LogoUrl = t.LogoUrl,
                PrimaryColor = t.PrimaryColor,
                SecondaryColor = t.SecondaryColor,
                Status = t.Status,
                Tier = t.Tier,
                ExpiresOn = t.ExpiresOn,
                CreatedOn = t.CreatedOn,
                ModifiedOn = t.ModifiedOn,
                Owner = t.Users.Where(ut => ut.Role == TeamRole.Host).Select(ut => new TeamMemberDto
                {
                    Id = ut.Id,
                    UserId = ut.UserId,
                    Email = ut.User.Email!,
                    FirstName = ut.User.FirstName,
                    LastName = ut.User.LastName,
                    DisplayName = $"{ut.User.LastName}, {ut.User.FirstName}",
                    Role = ut.Role,
                    JoinedOn = ut.JoinedOn,
                    IsActive = ut.IsActive,
                    IsOwner = true
                }).FirstOrDefault(),
                MemberCount = t.Users.Count(ut => ut.IsActive),
                AthleteCount = t.Users.Count(ut => ut.IsActive && ut.Role == TeamRole.Athlete),
                AdminCount = t.Users.Count(ut => ut.IsActive && (ut.Role == TeamRole.Admin || ut.Role == TeamRole.Host)),
                HasPendingOwnershipTransfer = _context.OwnershipTransfers.Any(ot => ot.TeamId == t.Id && ot.Status == OwnershipTransferStatus.Pending)
            })
            .FirstOrDefaultAsync();
    }

    // Additional authorization helper methods
    private async Task EnsureCanTransferOwnershipAsync(Guid teamId)
    {
        if (_currentUserService.IsGlobalAdmin)
            return;

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

        if (userTeam == null || userTeam.Role != TeamRole.Host)
        {
            throw new UnauthorizedAccessException("Team owner access required to transfer ownership");
        }
    }

    private async Task EnsureCanCancelTransferAsync(OwnershipTransfer transfer)
    {
        if (_currentUserService.IsGlobalAdmin)
            return;

        if (transfer.InitiatedByUserId != _currentUserService.UserId)
        {
            // Check if current user is team owner
            var userTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.TeamId == transfer.TeamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

            if (userTeam == null || userTeam.Role != TeamRole.Host)
            {
                throw new UnauthorizedAccessException("Only the transfer initiator or team owner can cancel the transfer");
            }
        }
    }

    private async Task EnsureCanManageSubscriptionAsync(Guid teamId)
    {
        if (_currentUserService.IsGlobalAdmin)
            return;

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

        if (userTeam == null || (userTeam.Role != TeamRole.Host && userTeam.Role != TeamRole.Admin))
        {
            throw new UnauthorizedAccessException("Admin access required to manage subscription");
        }
    }

    private async Task EnsureCanUpdateBrandingAsync(Guid teamId)
    {
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

        if (userTeam == null || userTeam.Role != TeamRole.Admin)
        {
            throw new UnauthorizedAccessException("Admin access required to update branding");
        }
    }

    private async Task EnsureCanManageMembersAsync(Guid teamId)
    {
        if (_currentUserService.IsGlobalAdmin)
            return;

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == _currentUserService.UserId && ut.IsActive);

        if (userTeam == null || (userTeam.Role != TeamRole.Host && userTeam.Role != TeamRole.Admin))
        {
            throw new UnauthorizedAccessException("Admin access required to manage members");
        }
    }

    private async Task<bool> CanDowngradeToTierAsync(Guid teamId, TeamTier newTier)
    {
        var limits = await GetTierLimitsAsync(newTier);
        
        var currentCounts = await _context.UserTeams
            .Where(ut => ut.TeamId == teamId && ut.IsActive)
            .GroupBy(ut => ut.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync();

        var athleteCount = currentCounts.FirstOrDefault(c => c.Role == TeamRole.Athlete)?.Count ?? 0;
        var adminCount = currentCounts.Where(c => c.Role == TeamRole.Admin || c.Role == TeamRole.Host).Sum(c => c.Count);
        var coachCount = currentCounts.FirstOrDefault(c => c.Role == TeamRole.Coach)?.Count ?? 0;

        return athleteCount <= limits.MaxAthletes && 
               adminCount <= limits.MaxAdmins && 
               coachCount <= limits.MaxCoaches;
    }
} 