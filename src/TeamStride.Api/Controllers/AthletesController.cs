using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Api.Authorization;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Athletes.Services;
using TeamStride.Application.Common.Models;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for athlete management operations
/// </summary>
[ApiController]
[Route("api/athletes")]
[Authorize]
public class AthletesController : BaseApiController
{
    private readonly IAthleteService _athleteService;

    public AthletesController(
        IAthleteService athleteService,
        ILogger<AthletesController> logger) : base(logger)
    {
        _athleteService = athleteService;
    }

    /// <summary>
    /// Gets a paginated list of athletes for the team
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of athletes</returns>
    [HttpGet]
    [RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(PaginatedList<AthleteDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetTeamRoster(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var athletes = await _athleteService.GetTeamRosterAsync(pageNumber, pageSize);
            return Ok(athletes);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve team roster");
        }
    }

    /// <summary>
    /// Gets an athlete by ID
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <returns>Athlete details</returns>
    [HttpGet("{id:guid}")]
    [RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAthlete(Guid id)
    {
        try
        {
            var athlete = await _athleteService.GetByIdAsync(id);
            return Ok(athlete);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to retrieve athlete {id}");
        }
    }

    /// <summary>
    /// Gets an athlete by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Athlete details if found, null otherwise</returns>
    [HttpGet("by-user/{userId:guid}")]
    [RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAthleteByUserId(Guid userId)
    {
        try
        {
            var athlete = await _athleteService.GetByUserIdAsync(userId);
            return athlete != null ? Ok(athlete) : NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to retrieve athlete for user {userId}");
        }
    }

    /// <summary>
    /// Gets team captains
    /// </summary>
    /// <returns>List of team captains</returns>
    [HttpGet("captains")]
    [RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(IEnumerable<AthleteDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetTeamCaptains()
    {
        try
        {
            var captains = await _athleteService.GetTeamCaptainsAsync();
            return Ok(captains);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve team captains");
        }
    }

    /// <summary>
    /// Creates a new athlete
    /// </summary>
    /// <param name="dto">Athlete creation data</param>
    /// <returns>Created athlete details</returns>
    [HttpPost]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateAthlete([FromBody] CreateAthleteDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var athlete = await _athleteService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAthlete), new { id = athlete.Id }, athlete);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to create athlete");
        }
    }

    /// <summary>
    /// Updates an athlete's basic properties
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <param name="dto">Athlete update data</param>
    /// <returns>Updated athlete details</returns>
    [HttpPut("{id:guid}")]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAthlete(Guid id, [FromBody] UpdateAthleteDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var athlete = await _athleteService.UpdateAsync(id, dto);
            return Ok(athlete);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to update athlete {id}");
        }
    }

    /// <summary>
    /// Updates an athlete's role
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <param name="role">New athlete role</param>
    /// <returns>Updated athlete details</returns>
    [HttpPatch("{id:guid}/role")]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAthleteRole(Guid id, [FromBody] AthleteRole role)
    {
        try
        {
            var athlete = await _athleteService.UpdateRoleAsync(id, role);
            return Ok(athlete);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to update athlete {id} role");
        }
    }

    /// <summary>
    /// Updates an athlete's physical status
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <param name="hasPhysical">Whether athlete has physical on file</param>
    /// <returns>Updated athlete details</returns>
    [HttpPatch("{id:guid}/physical")]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePhysicalStatus(Guid id, [FromBody] bool hasPhysical)
    {
        try
        {
            var athlete = await _athleteService.UpdatePhysicalStatusAsync(id, hasPhysical);
            return Ok(athlete);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to update athlete {id} physical status");
        }
    }

    /// <summary>
    /// Updates an athlete's waiver status
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <param name="hasSigned">Whether athlete has signed waiver</param>
    /// <returns>Updated athlete details</returns>
    [HttpPatch("{id:guid}/waiver")]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateWaiverStatus(Guid id, [FromBody] bool hasSigned)
    {
        try
        {
            var athlete = await _athleteService.UpdateWaiverStatusAsync(id, hasSigned);
            return Ok(athlete);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to update athlete {id} waiver status");
        }
    }

    /// <summary>
    /// Updates an athlete's profile
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <param name="profileDto">Athlete profile update data</param>
    /// <returns>Updated athlete details</returns>
    [HttpPut("{id:guid}/profile")]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(AthleteDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAthleteProfile(Guid id, [FromBody] UpdateAthleteProfileDto profileDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var athlete = await _athleteService.UpdateProfileAsync(id, profileDto);
            return Ok(athlete);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to update athlete {id} profile");
        }
    }

    /// <summary>
    /// Deletes an athlete
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAthlete(Guid id)
    {
        try
        {
            await _athleteService.DeleteAsync(id);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to delete athlete {id}");
        }
    }

    /// <summary>
    /// Checks if an athlete is in the current team
    /// </summary>
    /// <param name="id">Athlete ID</param>
    /// <returns>True if athlete is in team, false otherwise</returns>
    [HttpGet("{id:guid}/is-in-team")]
    [RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> IsAthleteInTeam(Guid id)
    {
        try
        {
            var isInTeam = await _athleteService.IsAthleteInTeamAsync(id);
            return Ok(isInTeam);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to check if athlete {id} is in team");
        }
    }
} 