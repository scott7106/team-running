using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Athletes.Services;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Data.Extensions;
using TeamStride.Infrastructure.Mapping;

namespace TeamStride.Infrastructure.Services;

public class AthleteService : IAthleteService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<AthleteService> _logger;

    public AthleteService(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<AthleteService> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AthleteDto> GetByIdAsync(Guid id)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamMember);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found");
        }

        return _mapper.Map<AthleteDto>(athlete);
    }

    public async Task<AthleteDto?> GetByUserIdAsync(Guid userId)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamMember);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return athlete != null ? _mapper.Map<AthleteDto>(athlete) : null;
    }

    public async Task<PaginatedList<AthleteDto>> GetTeamRosterAsync(int pageNumber = 1, int pageSize = 10)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamMember);

        var query = _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName);

        var totalCount = await query.CountAsync();
        var athletes = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var athleteDtos = _mapper.Map<List<AthleteDto>>(athletes);

        return new PaginatedList<AthleteDto>(athleteDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<AthleteDto> CreateAsync(CreateAthleteDto dto)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamAdmin);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            throw new InvalidOperationException("FirstName is required");
        }

        if (string.IsNullOrWhiteSpace(dto.LastName))
        {
            throw new InvalidOperationException("LastName is required");
        }

        // Check if team can add more athletes
        var canAddAthlete = await CanAddAthleteToTeamAsync();
        if (!canAddAthlete)
        {
            throw new InvalidOperationException("Team has reached the maximum number of athletes for its current tier");
        }

        ApplicationUser? user = null;

        // Only create/find user if email is provided
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                // Check if user is already an athlete on this team
                var existingAthlete = await _context.Athletes
                    .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
                    .FirstOrDefaultAsync(a => a.UserId == existingUser.Id);

                if (existingAthlete != null)
                {
                    throw new InvalidOperationException($"User with email {dto.Email} is already an athlete on this team");
                }

                user = existingUser;
            }
            else
            {
                // Create new user
                user = _mapper.Map<ApplicationUser>(dto);
                user.EmailConfirmed = true; // Assuming email confirmation is not required for athletes added by coaches
                
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user: {errors}");
                }
            }
        }

        // Create athlete record
        var athlete = _mapper.Map<Athlete>(dto);
        athlete.UserId = user?.Id;
        athlete.TeamId = _currentUserService.CurrentTeamId.Value;
        athlete.CreatedOn = DateTime.UtcNow;
        athlete.CreatedBy = _currentUserService.UserId;

        // Create athlete profile if provided
        if (dto.Profile != null)
        {
            athlete.Profile = _mapper.Map<AthleteProfile>(dto.Profile);
            athlete.Profile.AthleteId = athlete.Id;
            athlete.Profile.TeamId = _currentUserService.CurrentTeamId.Value;
            athlete.Profile.CreatedOn = DateTime.UtcNow;
            athlete.Profile.CreatedBy = _currentUserService.UserId;
        }

        _context.Athletes.Add(athlete);

        // Create team membership only if user was created/found
        if (user != null)
        {
            var userTeam = new UserTeam
            {
                UserId = user.Id,
                TeamId = _currentUserService.CurrentTeamId.Value,
                Role = TeamRole.TeamMember,
                MemberType = MemberType.Athlete,
                IsActive = true,
                JoinedOn = DateTime.UtcNow
            };
            _context.UserTeams.Add(userTeam);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created athlete {AthleteId} for user {UserId} on team {TeamId}", 
            athlete.Id, user?.Id, _currentUserService.CurrentTeamId);

        // Return the created athlete
        var createdAthlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstAsync(a => a.Id == athlete.Id);

        return _mapper.Map<AthleteDto>(createdAthlete);
    }

    public async Task<AthleteDto> UpdateAsync(Guid id, UpdateAthleteDto dto)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamAdmin);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found");
        }

        // Update athlete properties
        _mapper.Map(dto, athlete);
        athlete.ModifiedOn = DateTime.UtcNow;
        athlete.ModifiedBy = _currentUserService.UserId;

        // Update profile if provided
        if (dto.Profile != null)
        {
            if (athlete.Profile == null)
            {
                athlete.Profile = new AthleteProfile
                {
                    AthleteId = athlete.Id,
                    TeamId = _currentUserService.CurrentTeamId.Value,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId
                };
                _context.AthleteProfiles.Add(athlete.Profile);
            }

            _mapper.Map(dto.Profile, athlete.Profile);
            athlete.Profile.ModifiedOn = DateTime.UtcNow;
            athlete.Profile.ModifiedBy = _currentUserService.UserId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated athlete {AthleteId} on team {TeamId}", id, _currentUserService.CurrentTeamId);

        return _mapper.Map<AthleteDto>(athlete);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamAdmin);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found");
        }

        // Remove athlete profile if exists
        if (athlete.Profile != null)
        {
            _context.AthleteProfiles.Remove(athlete.Profile);
        }

        // Remove athlete
        _context.Athletes.Remove(athlete);

        // Remove team membership only if the athlete has a user account
        if (athlete.UserId.HasValue)
        {
            var userTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.UserId == athlete.UserId.Value && 
                                         ut.TeamId == _currentUserService.CurrentTeamId && 
                                         ut.MemberType == MemberType.Athlete);
            if (userTeam != null)
            {
                _context.UserTeams.Remove(userTeam);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted athlete {AthleteId} from team {TeamId}", id, _currentUserService.CurrentTeamId);
    }

    public async Task<AthleteDto> UpdateRoleAsync(Guid id, AthleteRole role)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamAdmin);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found");
        }

        athlete.Role = role;
        athlete.ModifiedOn = DateTime.UtcNow;
        athlete.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated athlete {AthleteId} role to {Role} on team {TeamId}", 
            id, role, _currentUserService.CurrentTeamId);

        return _mapper.Map<AthleteDto>(athlete);
    }

    public async Task<AthleteDto> UpdatePhysicalStatusAsync(Guid id, bool hasPhysical)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamAdmin);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found");
        }

        athlete.HasPhysicalOnFile = hasPhysical;
        athlete.ModifiedOn = DateTime.UtcNow;
        athlete.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated athlete {AthleteId} physical status to {HasPhysical} on team {TeamId}", 
            id, hasPhysical, _currentUserService.CurrentTeamId);

        return _mapper.Map<AthleteDto>(athlete);
    }

    public async Task<AthleteDto> UpdateWaiverStatusAsync(Guid id, bool hasSigned)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamAdmin);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found");
        }

        athlete.HasWaiverSigned = hasSigned;
        athlete.ModifiedOn = DateTime.UtcNow;
        athlete.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated athlete {AthleteId} waiver status to {HasSigned} on team {TeamId}", 
            id, hasSigned, _currentUserService.CurrentTeamId);

        return _mapper.Map<AthleteDto>(athlete);
    }

    public async Task<AthleteDto> UpdateProfileAsync(Guid id, UpdateAthleteProfileDto profileDto)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamAdmin);

        var athlete = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found");
        }

        if (athlete.Profile == null)
        {
            athlete.Profile = new AthleteProfile
            {
                AthleteId = athlete.Id,
                TeamId = _currentUserService.CurrentTeamId.Value,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId
            };
            _context.AthleteProfiles.Add(athlete.Profile);
        }

        _mapper.Map(profileDto, athlete.Profile);
        athlete.Profile.ModifiedOn = DateTime.UtcNow;
        athlete.Profile.ModifiedBy = _currentUserService.UserId;

        athlete.ModifiedOn = DateTime.UtcNow;
        athlete.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated athlete {AthleteId} profile on team {TeamId}", 
            id, _currentUserService.CurrentTeamId);

        return _mapper.Map<AthleteDto>(athlete);
    }

    public async Task<IEnumerable<AthleteDto>> GetTeamCaptainsAsync()
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamMember);

        var captains = await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .Where(a => a.Role == AthleteRole.Captain)
            .Include(a => a.User)
            .Include(a => a.Profile)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync();

        return _mapper.Map<IEnumerable<AthleteDto>>(captains);
    }

    public async Task<bool> IsAthleteInTeamAsync(Guid athleteId)
    {
        await _authorizationService.RequireTeamAccessAsync(_currentUserService.CurrentTeamId!.Value, TeamRole.TeamMember);

        return await _context.Athletes
            .ApplyTeamIdFilter(_currentUserService.CurrentTeamId)
            .AnyAsync(a => a.Id == athleteId);
    }

    private async Task<bool> CanAddAthleteToTeamAsync()
    {
        var team = await _context.Teams
            .ApplyTeamFilter(_currentUserService.CurrentTeamId)
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == _currentUserService.CurrentTeamId);

        if (team == null)
        {
            return false;
        }

        var tierLimits = GetTierLimits(team.Tier);
        var currentAthleteCount = team.Users
            .Count(ut => ut.TeamId == _currentUserService.CurrentTeamId && 
                        ut.IsActive && 
                        ut.MemberType == MemberType.Athlete);

        return currentAthleteCount < tierLimits.MaxAthletes;
    }

    private static TeamTierLimitsDto GetTierLimits(TeamTier tier)
    {
        return tier switch
        {
            TeamTier.Free => new TeamTierLimitsDto { MaxAthletes = 7, MaxAdmins = 2 },
            TeamTier.Standard => new TeamTierLimitsDto { MaxAthletes = 30, MaxAdmins = 5 },
            TeamTier.Premium => new TeamTierLimitsDto { MaxAthletes = int.MaxValue, MaxAdmins = int.MaxValue },
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Invalid team tier")
        };
    }
} 