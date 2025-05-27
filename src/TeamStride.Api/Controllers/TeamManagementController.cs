using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for team management operations including CRUD, ownership transfers, subscriptions, and member management
/// </summary>
[Authorize]
public class TeamManagementController : BaseApiController
{
    private readonly ITeamManagementService _teamManagementService;

    public TeamManagementController(
        ITeamManagementService teamManagementService,
        ILogger<TeamManagementController> logger) : base(logger)
    {
        _teamManagementService = teamManagementService;
    }

    /// <summary>
    /// Gets a paginated list of teams (Global Admin only)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="searchQuery">Search query for team name or subdomain</param>
    /// <param name="status">Filter by team status</param>
    /// <param name="tier">Filter by team tier</param>
    /// <returns>Paginated list of teams</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TeamManagementDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetTeams(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchQuery = null,
        [FromQuery] TeamStatus? status = null,
        [FromQuery] TeamTier? tier = null)
    {
        try
        {
            var teams = await _teamManagementService.GetTeamsAsync(pageNumber, pageSize, searchQuery, status, tier);
            return Ok(teams);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve teams");
        }
    }

    /// <summary>
    /// Gets a team by ID
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Team details</returns>
    [HttpGet("{teamId:guid}")]
    [ProducesResponseType(typeof(TeamManagementDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTeamById(Guid teamId)
    {
        try
        {
            var team = await _teamManagementService.GetTeamByIdAsync(teamId);
            return Ok(team);
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
            return HandleError(ex, "Failed to retrieve team");
        }
    }

    /// <summary>
    /// Gets a team by subdomain
    /// </summary>
    /// <param name="subdomain">Team subdomain</param>
    /// <returns>Team details</returns>
    [HttpGet("subdomain/{subdomain}")]
    [ProducesResponseType(typeof(TeamManagementDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTeamBySubdomain(string subdomain)
    {
        try
        {
            var team = await _teamManagementService.GetTeamBySubdomainAsync(subdomain);
            return Ok(team);
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
            return HandleError(ex, "Failed to retrieve team");
        }
    }

    /// <summary>
    /// Creates a new team (Global Admin only)
    /// </summary>
    /// <param name="dto">Team creation data</param>
    /// <returns>Created team details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TeamManagementDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _teamManagementService.CreateTeamAsync(dto);
            return CreatedAtAction(nameof(GetTeamById), new { teamId = team.Id }, team);
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
            return HandleError(ex, "Failed to create team");
        }
    }

    /// <summary>
    /// Updates a team
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Team update data</param>
    /// <returns>Updated team details</returns>
    [HttpPut("{teamId:guid}")]
    [ProducesResponseType(typeof(TeamManagementDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateTeam(Guid teamId, [FromBody] UpdateTeamDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _teamManagementService.UpdateTeamAsync(teamId, dto);
            return Ok(team);
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
            return HandleError(ex, "Failed to update team");
        }
    }

    /// <summary>
    /// Deletes a team (soft delete)
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{teamId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteTeam(Guid teamId)
    {
        try
        {
            await _teamManagementService.DeleteTeamAsync(teamId);
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
            return HandleError(ex, "Failed to delete team");
        }
    }

    /// <summary>
    /// Checks if a subdomain is available
    /// </summary>
    /// <param name="subdomain">Subdomain to check</param>
    /// <returns>Availability status</returns>
    [HttpGet("subdomain/{subdomain}/availability")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<IActionResult> CheckSubdomainAvailability(string subdomain)
    {
        try
        {
            var isAvailable = await _teamManagementService.IsSubdomainAvailableAsync(subdomain);
            return Ok(new { subdomain, isAvailable });
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to check subdomain availability");
        }
    }

    /// <summary>
    /// Updates a team's subdomain (Global Admin only)
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="newSubdomain">New subdomain</param>
    /// <returns>Updated team details</returns>
    [HttpPut("{teamId:guid}/subdomain")]
    [ProducesResponseType(typeof(TeamManagementDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSubdomain(Guid teamId, [FromBody] string newSubdomain)
    {
        if (string.IsNullOrWhiteSpace(newSubdomain))
        {
            return BadRequest("Subdomain cannot be empty");
        }

        try
        {
            var team = await _teamManagementService.UpdateSubdomainAsync(teamId, newSubdomain);
            return Ok(team);
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
            return HandleError(ex, "Failed to update subdomain");
        }
    }

    /// <summary>
    /// Gets tier limits for a specific tier
    /// </summary>
    /// <param name="tier">Team tier</param>
    /// <returns>Tier limits</returns>
    [HttpGet("tiers/{tier}/limits")]
    [ProducesResponseType(typeof(TeamTierLimitsDto), 200)]
    public async Task<IActionResult> GetTierLimits(TeamTier tier)
    {
        try
        {
            var limits = await _teamManagementService.GetTierLimitsAsync(tier);
            return Ok(limits);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve tier limits");
        }
    }

    /// <summary>
    /// Checks if a team can add more athletes
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Whether the team can add more athletes</returns>
    [HttpGet("{teamId:guid}/can-add-athlete")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CanAddAthlete(Guid teamId)
    {
        try
        {
            var canAdd = await _teamManagementService.CanAddAthleteAsync(teamId);
            return Ok(new { teamId, canAddAthlete = canAdd });
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to check athlete capacity");
        }
    }
} 