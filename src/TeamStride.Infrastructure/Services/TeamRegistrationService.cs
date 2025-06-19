using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Services;

public class TeamRegistrationService : ITeamRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly TeamStride.Application.Common.Services.IAuthorizationService _authorizationService;
    private readonly TeamStride.Application.Common.Services.IEmailService _emailService;
    private readonly ILogger<TeamRegistrationService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeamRegistrationService(
        ApplicationDbContext context,
        IMapper mapper,
        ICurrentUserService currentUserService,
        TeamStride.Application.Common.Services.IAuthorizationService authorizationService,
        TeamStride.Application.Common.Services.IEmailService emailService,
        ILogger<TeamRegistrationService> logger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _emailService = emailService;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<TeamRegistrationWindowDto> CreateRegistrationWindowAsync(Guid teamId, CreateRegistrationWindowDto dto)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue || !await _authorizationService.CanManageRegistrationAsync(teamId, userId.Value))
        {
            throw new UnauthorizedAccessException("You are not authorized to manage registration windows for this team");
        }

        // Validate dates
        if (dto.StartDate >= dto.EndDate)
        {
            throw new InvalidOperationException("Start date must be before end date");
        }

        // Check for overlapping windows
        var hasOverlap = await _context.TeamRegistrationWindows
            .AnyAsync(w => w.TeamId == teamId && 
                ((w.StartDate <= dto.StartDate && w.EndDate >= dto.StartDate) ||
                 (w.StartDate <= dto.EndDate && w.EndDate >= dto.EndDate) ||
                 (w.StartDate >= dto.StartDate && w.EndDate <= dto.EndDate)));

        if (hasOverlap)
        {
            throw new InvalidOperationException("Registration window overlaps with existing window");
        }

        var window = new TeamRegistrationWindow
        {
            TeamId = teamId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            MaxRegistrations = dto.MaxRegistrations,
            RegistrationPasscode = dto.RegistrationPasscode,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };

        _context.TeamRegistrationWindows.Add(window);
        await _context.SaveChangesAsync();

        return _mapper.Map<TeamRegistrationWindowDto>(window);
    }

    public async Task<TeamRegistrationWindowDto> UpdateRegistrationWindowAsync(Guid teamId, Guid windowId, UpdateRegistrationWindowDto dto)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var window = await _context.TeamRegistrationWindows
            .Include(w => w.Team)
            .FirstOrDefaultAsync(w => w.Id == windowId && w.TeamId == teamId)
            ?? throw new InvalidOperationException("Registration window not found");

        // Validate dates
        if (dto.StartDate >= dto.EndDate)
        {
            throw new InvalidOperationException("Start date must be before end date");
        }

        // Check for overlapping windows (excluding current window)
        var hasOverlap = await _context.TeamRegistrationWindows
            .AnyAsync(w => w.TeamId == teamId && w.Id != windowId &&
                ((w.StartDate <= dto.StartDate && w.EndDate >= dto.StartDate) ||
                 (w.StartDate <= dto.EndDate && w.EndDate >= dto.EndDate) ||
                 (w.StartDate >= dto.StartDate && w.EndDate <= dto.EndDate)));

        if (hasOverlap)
        {
            throw new InvalidOperationException("Registration window overlaps with existing window");
        }

        window.StartDate = dto.StartDate;
        window.EndDate = dto.EndDate;
        window.MaxRegistrations = dto.MaxRegistrations;
        window.RegistrationPasscode = dto.RegistrationPasscode;
        window.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _mapper.Map<TeamRegistrationWindowDto>(window);
    }

    public async Task<List<TeamRegistrationWindowDto>> GetRegistrationWindowsAsync(Guid teamId)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var windows = await _context.TeamRegistrationWindows
            .Include(w => w.Team)
            .Where(w => w.TeamId == teamId)
            .OrderByDescending(w => w.StartDate)
            .ToListAsync();

        return _mapper.Map<List<TeamRegistrationWindowDto>>(windows);
    }

    public async Task<TeamRegistrationWindowDto> GetActiveRegistrationWindowAsync(Guid teamId)
    {
        var now = DateTime.UtcNow;
        var window = await _context.TeamRegistrationWindows
            .Include(w => w.Team)
            .FirstOrDefaultAsync(w => w.TeamId == teamId && 
                w.IsActive && 
                w.StartDate <= now && 
                w.EndDate >= now);

        return window != null ? _mapper.Map<TeamRegistrationWindowDto>(window) : null!;
    }

    public async Task<TeamRegistrationDto> SubmitRegistrationAsync(Guid teamId, SubmitRegistrationDto dto)
    {
        // Validate registration window
        var window = await GetActiveRegistrationWindowAsync(teamId);
        if (window == null)
        {
            throw new InvalidOperationException("No active registration window found");
        }

        // Validate passcode
        if (!await ValidateRegistrationPasscodeAsync(teamId, dto.RegistrationPasscode))
        {
            throw new InvalidOperationException("Invalid registration passcode");
        }

        // Check available spots
        var currentRegistrations = await _context.TeamRegistrations
            .CountAsync(r => r.TeamId == teamId && r.Status == RegistrationStatus.Approved);

        var registration = new TeamRegistration
        {
            TeamId = teamId,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            CodeOfConductAccepted = dto.CodeOfConductAccepted,
            CodeOfConductAcceptedOn = DateTime.UtcNow,
            Status = currentRegistrations < window.MaxRegistrations ? 
                RegistrationStatus.Pending : 
                RegistrationStatus.Waitlisted,
            CreatedOn = DateTime.UtcNow
        };

        // Add athletes
        foreach (var athleteDto in dto.Athletes)
        {
            var athleteRegistration = new AthleteRegistration
            {
                FirstName = athleteDto.FirstName,
                LastName = athleteDto.LastName,
                Birthdate = athleteDto.Birthdate,
                GradeLevel = athleteDto.GradeLevel,
                CreatedOn = DateTime.UtcNow
            };

            registration.Athletes.Add(athleteRegistration);
        }

        _context.TeamRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        // Send confirmation email
        var registrationDto = _mapper.Map<TeamRegistrationDto>(registration);
        await _emailService.SendRegistrationConfirmationAsync(registrationDto);

        return registrationDto;
    }

    public async Task<TeamRegistrationDto> UpdateRegistrationStatusAsync(Guid teamId, Guid registrationId, UpdateRegistrationStatusDto dto)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var registration = await _context.TeamRegistrations
            .Include(r => r.Athletes)
            .Include(r => r.Team)
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.TeamId == teamId)
            ?? throw new InvalidOperationException("Registration not found");

        var oldStatus = registration.Status;
        registration.Status = dto.Status;
        registration.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send status update email
        var registrationDto = _mapper.Map<TeamRegistrationDto>(registration);
        await _emailService.SendRegistrationStatusUpdateAsync(registrationDto);

        // If approved, create user account and team memberships
        if (dto.Status == RegistrationStatus.Approved && oldStatus != RegistrationStatus.Approved)
        {
            await CreateUserAndTeamMembershipsAsync(registration);
        }

        return registrationDto;
    }

    public async Task<List<TeamRegistrationDto>> GetRegistrationsAsync(Guid teamId)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var registrations = await _context.TeamRegistrations
            .Include(r => r.Athletes)
            .Include(r => r.Team)
            .Where(r => r.TeamId == teamId)
            .OrderByDescending(r => r.CreatedOn)
            .ToListAsync();

        return _mapper.Map<List<TeamRegistrationDto>>(registrations);
    }

    public async Task<List<TeamRegistrationDto>> GetWaitlistAsync(Guid teamId)
    {
        await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);

        var waitlist = await _context.TeamRegistrations
            .Include(r => r.Athletes)
            .Include(r => r.Team)
            .Where(r => r.TeamId == teamId && r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.CreatedOn)
            .ToListAsync();

        return _mapper.Map<List<TeamRegistrationDto>>(waitlist);
    }

    public async Task<bool> ValidateRegistrationPasscodeAsync(Guid teamId, string passcode)
    {
        var window = await GetActiveRegistrationWindowAsync(teamId);
        return window != null && window.RegistrationPasscode == passcode;
    }

    public async Task<bool> IsRegistrationWindowOpenAsync(Guid teamId)
    {
        var window = await GetActiveRegistrationWindowAsync(teamId);
        return window != null;
    }

    public async Task<bool> HasAvailableSpotsAsync(Guid teamId)
    {
        var window = await GetActiveRegistrationWindowAsync(teamId);
        if (window == null) return false;

        var currentRegistrations = await _context.TeamRegistrations
            .CountAsync(r => r.TeamId == teamId && r.Status == RegistrationStatus.Approved);

        return currentRegistrations < window.MaxRegistrations;
    }

    private async Task CreateUserAndTeamMembershipsAsync(TeamRegistration registration)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        // Create primary user account
        var user = new ApplicationUser
        {
            UserName = registration.Email,
            Email = registration.Email,
            FirstName = registration.FirstName,
            LastName = registration.LastName,
            EmailConfirmed = true
        };

        var password = GenerateRandomPassword();
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user account: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Create team membership for primary user
        var userTeam = new UserTeam
        {
            TeamId = registration.TeamId,
            UserId = user.Id,
            Role = TeamRole.TeamMember,
            MemberType = MemberType.Parent,
            IsActive = true,
            JoinedOn = DateTime.UtcNow
        };
        _context.UserTeams.Add(userTeam);

        // Create athlete accounts and memberships
        if (registration.Athletes != null)
        {
            foreach (var athlete in registration.Athletes)
            {
                var athleteUser = new ApplicationUser
                {
                    UserName = $"{athlete.FirstName.ToLower()}.{athlete.LastName.ToLower()}.{Guid.NewGuid()}",
                    Email = registration.Email, // Use parent's email
                    FirstName = athlete.FirstName,
                    LastName = athlete.LastName,
                    EmailConfirmed = true
                };

                var athletePassword = GenerateRandomPassword();
                result = await _userManager.CreateAsync(athleteUser, athletePassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create athlete account: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                var athleteTeam = new UserTeam
                {
                    TeamId = registration.TeamId,
                    UserId = athleteUser.Id,
                    Role = TeamRole.TeamMember,
                    MemberType = MemberType.Athlete,
                    IsActive = true,
                    JoinedOn = DateTime.UtcNow
                };
                _context.UserTeams.Add(athleteTeam);

                // Create athlete profile
                // TODO: Update this section when team registration integration is implemented
                var athleteProfile = new Athlete
                {
                    UserId = athleteUser.Id,
                    TeamId = registration.TeamId,
                    FirstName = athlete.FirstName,
                    LastName = athlete.LastName,
                    DateOfBirth = athlete.Birthdate,
                    GradeLevel = GradeLevel.Other, // Temporary default value
                    Gender = Gender.NS, // Temporary default value
                    CreatedOn = DateTime.UtcNow
                };
                _context.Athletes.Add(athleteProfile);
            }
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";
        var random = new Random();
        var password = new char[12];
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        return new string(password);
    }
} 