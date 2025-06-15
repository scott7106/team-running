using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Api.Authorization;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for core team management operations
/// </summary>
[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController : BaseApiController
{
    private readonly IStandardTeamService _standardTeamService;

    public TeamsController(
        IStandardTeamService standardTeamService,
        ILogger<TeamsController> logger) : base(logger)
    {
        _standardTeamService = standardTeamService;
    }

    /// <summary>
    /// Gets a paginated list of teams the user has access to
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="searchQuery">Search query for team name or subdomain</param>
    /// <param name="status">Filter by team status</param>
    /// <param name="tier">Filter by team tier</param>
    /// <returns>Paginated list of teams</returns>
    [HttpGet]
    [RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(PaginatedList<TeamDto>), 200)]
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
            var teams = await _standardTeamService.GetTeamsAsync(pageNumber, pageSize, searchQuery, status, tier);
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
    [RequireTeamAccess(TeamRole.TeamMember)]
    [ProducesResponseType(typeof(TeamDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTeamById(Guid teamId)
    {
        try
        {
            var team = await _standardTeamService.GetTeamByIdAsync(teamId);
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
            return HandleError(ex, $"Failed to retrieve team {teamId}");
        }
    }

    /// <summary>
    /// Gets a team by subdomain
    /// </summary>
    /// <param name="subdomain">Team subdomain</param>
    /// <returns>Team details</returns>
    [HttpGet("subdomain/{subdomain}")]
    [RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
    [ProducesResponseType(typeof(TeamDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTeamBySubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return BadRequest("Subdomain is required");
        }

        try
        {
            var team = await _standardTeamService.GetTeamBySubdomainAsync(subdomain);
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
            return HandleError(ex, $"Failed to retrieve team with subdomain {subdomain}");
        }
    }



    /// <summary>
    /// Updates a team's basic properties
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Team update data</param>
    /// <returns>Updated team details</returns>
    [HttpPut("{teamId:guid}")]
    [RequireTeamAccess(TeamRole.TeamAdmin)]
    [ProducesResponseType(typeof(TeamDto), 200)]
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
            var team = await _standardTeamService.UpdateTeamAsync(teamId, dto);
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
            return HandleError(ex, $"Failed to update team {teamId}");
        }
    }





    /// <summary>
    /// Updates a team's subdomain
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="newSubdomain">New subdomain</param>
    /// <returns>Updated team details</returns>
    [HttpPut("{teamId:guid}/subdomain")]
    [RequireTeamAccess(TeamRole.TeamOwner)]
    [ProducesResponseType(typeof(TeamDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSubdomain(Guid teamId, [FromBody] string newSubdomain)
    {
        if (string.IsNullOrWhiteSpace(newSubdomain))
        {
            return BadRequest("New subdomain is required");
        }

        try
        {
            var team = await _standardTeamService.UpdateSubdomainAsync(teamId, newSubdomain);
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
            return HandleError(ex, $"Failed to update subdomain for team {teamId}");
        }
    }

    /// <summary>
    /// Gets tier limits for a specific tier
    /// </summary>
    /// <param name="tier">Team tier</param>
    /// <returns>Tier limits and features</returns>
    [HttpGet("tiers/{tier}/limits")]
    [ProducesResponseType(typeof(TeamTierLimitsDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetTierLimits(TeamTier tier)
    {
        try
        {
            var limits = await _standardTeamService.GetTierLimitsAsync(tier);
            return Ok(limits);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to get tier limits for {tier}");
        }
    }

    /// <summary>
    /// Checks if a team can add more athletes based on tier limits
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
            var canAdd = await _standardTeamService.CanAddAthleteAsync(teamId);
            return Ok(canAdd);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to check athlete capacity for team {teamId}");
        }
    }
} 