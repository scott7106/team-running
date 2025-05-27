using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Athletes.Services;
using TeamStride.Application.Common.Models;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Data.Extensions;

namespace TeamStride.Infrastructure.Services;

public class AthleteService : IAthleteService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentTeamService _teamService;
    private readonly ICurrentUserService _currentUserService;

    public AthleteService(
        ApplicationDbContext context,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        ICurrentTeamService teamService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
        _teamService = teamService;
        _currentUserService = currentUserService;
    }

    public async Task<AthleteDto> GetByIdAsync(Guid id)
    {
        var athlete = await GetAthleteByIdOrThrowAsync(id);
        return _mapper.Map<AthleteDto>(athlete);
    }

    public async Task<AthleteDto?> GetByUserIdAsync(Guid userId)
    {
        var athlete = await _context.Athletes
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.TeamId == _teamService.TeamId);

        return athlete == null ? null : _mapper.Map<AthleteDto>(athlete);
    }

    public async Task<PaginatedList<AthleteDto>> GetTeamRosterAsync(int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Athletes
            .Include(a => a.User)
            .Include(a => a.Profile)
            .Where(a => a.TeamId == _teamService.TeamId)
            .OrderBy(a => a.User.LastName)
            .ThenBy(a => a.User.FirstName);

        var athletes = await PaginatedList<Athlete>.CreateAsync(query, pageNumber, pageSize);
        
        return new PaginatedList<AthleteDto>(
            _mapper.Map<List<AthleteDto>>(athletes.Items),
            athletes.TotalCount,
            athletes.PageNumber,
            pageSize);
    }

    public async Task<AthleteDto> CreateAsync(CreateAthleteDto dto)
    {
        // Create user
        var user = _mapper.Map<ApplicationUser>(dto);
        var result = await _userManager.CreateAsync(user);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Create athlete
        var athlete = _mapper.Map<Athlete>(dto);
        athlete.UserId = user.Id;
        athlete.TeamId = _teamService.TeamId;

        // Create profile if provided
        if (dto.Profile != null)
        {
            athlete.Profile = _mapper.Map<AthleteProfile>(dto.Profile);
        }

        _context.Athletes.Add(athlete);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(athlete.Id);
    }

    public async Task<AthleteDto> UpdateAsync(Guid id, UpdateAthleteDto dto)
    {
        var athlete = await GetAthleteByIdOrThrowAsync(id);
        _mapper.Map(dto, athlete);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var athlete = await GetAthleteByIdOrThrowAsync(id);
        athlete.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<AthleteDto> UpdateRoleAsync(Guid id, AthleteRole role)
    {
        var athlete = await GetAthleteByIdOrThrowAsync(id);
        athlete.Role = role;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<AthleteDto> UpdatePhysicalStatusAsync(Guid id, bool hasPhysical)
    {
        var athlete = await GetAthleteByIdOrThrowAsync(id);
        athlete.HasPhysicalOnFile = hasPhysical;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<AthleteDto> UpdateWaiverStatusAsync(Guid id, bool hasSigned)
    {
        var athlete = await GetAthleteByIdOrThrowAsync(id);
        athlete.HasWaiverSigned = hasSigned;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<AthleteDto> UpdateProfileAsync(Guid id, UpdateAthleteProfileDto profile)
    {
        var athlete = await GetAthleteByIdOrThrowAsync(id);
        _mapper.Map(profile, athlete.Profile);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<IEnumerable<AthleteDto>> GetTeamCaptainsAsync()
    {
        var captains = await _context.Athletes
            .Include(a => a.User)
            .Include(a => a.Profile)
            .Where(a => a.TeamId == _teamService.TeamId && a.Role == AthleteRole.Captain)
            .OrderBy(a => a.User.LastName)
            .ThenBy(a => a.User.FirstName)
            .ToListAsync();

        return _mapper.Map<IEnumerable<AthleteDto>>(captains);
    }

    public async Task<bool> IsAthleteInTeamAsync(Guid athleteId)
    {
        return await _context.Athletes
            .AnyAsync(a => a.Id == athleteId && a.TeamId == _teamService.TeamId);
    }

    private async Task<Athlete> GetAthleteByIdOrThrowAsync(Guid id)
    {
        var athlete = await _context.Athletes
            .Include(a => a.User)
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.Id == id && a.TeamId == _teamService.TeamId);

        if (athlete == null)
        {
            throw new InvalidOperationException($"Athlete with ID {id} not found in current team");
        }

        return athlete;
    }
} 