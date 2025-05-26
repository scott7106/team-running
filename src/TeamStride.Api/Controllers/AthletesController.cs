using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Athletes.Services;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Manages athlete-related operations including roster management, profile updates, and status tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AthletesController : ControllerBase
{
    private readonly IAthleteService _athleteService;

    public AthletesController(IAthleteService athleteService)
    {
        _athleteService = athleteService;
    }

    /// <summary>
    /// Retrieves a paginated list of athletes in the current team's roster
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of athletes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AthleteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<AthleteDto>>> GetRoster(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var roster = await _athleteService.GetTeamRosterAsync(pageNumber, pageSize);
        return Ok(roster);
    }

    /// <summary>
    /// Retrieves a specific athlete by their ID
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    /// <returns>The athlete details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AthleteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AthleteDto>> GetById(Guid id)
    {
        var athlete = await _athleteService.GetByIdAsync(id);
        return Ok(athlete);
    }

    /// <summary>
    /// Creates a new athlete in the current team
    /// </summary>
    /// <param name="dto">The athlete creation data</param>
    /// <returns>The newly created athlete</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Coach")]
    [ProducesResponseType(typeof(AthleteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AthleteDto>> Create([FromBody] CreateAthleteDto dto)
    {
        var athlete = await _athleteService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = athlete.Id }, athlete);
    }

    /// <summary>
    /// Updates an existing athlete's information
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    /// <param name="dto">The updated athlete data</param>
    /// <returns>The updated athlete</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Coach")]
    [ProducesResponseType(typeof(AthleteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AthleteDto>> Update(Guid id, [FromBody] UpdateAthleteDto dto)
    {
        var athlete = await _athleteService.UpdateAsync(id, dto);
        return Ok(athlete);
    }

    /// <summary>
    /// Removes an athlete from the team
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _athleteService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Updates an athlete's role (Athlete/Captain)
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    /// <param name="role">The new role</param>
    /// <returns>The updated athlete</returns>
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "Admin,Coach")]
    [ProducesResponseType(typeof(AthleteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AthleteDto>> UpdateRole(Guid id, [FromBody] AthleteRole role)
    {
        var athlete = await _athleteService.UpdateRoleAsync(id, role);
        return Ok(athlete);
    }

    /// <summary>
    /// Updates an athlete's physical form status
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    /// <param name="hasPhysical">Whether the physical form is on file</param>
    /// <returns>The updated athlete</returns>
    [HttpPut("{id:guid}/physical-status")]
    [Authorize(Roles = "Admin,Coach")]
    [ProducesResponseType(typeof(AthleteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AthleteDto>> UpdatePhysicalStatus(Guid id, [FromBody] bool hasPhysical)
    {
        var athlete = await _athleteService.UpdatePhysicalStatusAsync(id, hasPhysical);
        return Ok(athlete);
    }

    /// <summary>
    /// Updates an athlete's waiver signed status
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    /// <param name="hasSigned">Whether the waiver has been signed</param>
    /// <returns>The updated athlete</returns>
    [HttpPut("{id:guid}/waiver-status")]
    [Authorize(Roles = "Admin,Coach")]
    [ProducesResponseType(typeof(AthleteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AthleteDto>> UpdateWaiverStatus(Guid id, [FromBody] bool hasSigned)
    {
        var athlete = await _athleteService.UpdateWaiverStatusAsync(id, hasSigned);
        return Ok(athlete);
    }

    /// <summary>
    /// Updates an athlete's profile information
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    /// <param name="profile">The updated profile data</param>
    /// <returns>The updated athlete</returns>
    [HttpPut("{id:guid}/profile")]
    [Authorize(Roles = "Admin,Coach")]
    [ProducesResponseType(typeof(AthleteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AthleteDto>> UpdateProfile(Guid id, [FromBody] UpdateAthleteProfileDto profile)
    {
        var athlete = await _athleteService.UpdateProfileAsync(id, profile);
        return Ok(athlete);
    }

    /// <summary>
    /// Retrieves all team captains
    /// </summary>
    /// <returns>List of team captains</returns>
    [HttpGet("captains")]
    [ProducesResponseType(typeof(IEnumerable<AthleteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AthleteDto>>> GetCaptains()
    {
        var captains = await _athleteService.GetTeamCaptainsAsync();
        return Ok(captains);
    }

    /// <summary>
    /// Checks if an athlete is a member of the current team
    /// </summary>
    /// <param name="id">The athlete's unique identifier</param>
    /// <returns>True if the athlete is in the team, false otherwise</returns>
    [HttpGet("{id:guid}/is-in-team")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> IsInTeam(Guid id)
    {
        var isInTeam = await _athleteService.IsAthleteInTeamAsync(id);
        return Ok(isInTeam);
    }
} 