using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Api.Authorization;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Global admin controller for team management operations.
/// All endpoints require global admin privileges and bypass normal team access restrictions.
/// </summary>
[ApiController]
[Route("api/admin/teams")]
[Authorize]
[RequireGlobalAdmin]
public class GlobalAdminTeamsController : BaseApiController
{
    private readonly IGlobalAdminTeamService _globalAdminTeamService;

    public GlobalAdminTeamsController(
        IGlobalAdminTeamService globalAdminTeamService,
        ILogger<GlobalAdminTeamsController> logger) : base(logger)
    {
        _globalAdminTeamService = globalAdminTeamService;
    }

    /// <summary>
    /// Gets a paginated list of all teams in the system with search and filtering capabilities.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by team name, subdomain, or owner email</param>
    /// <param name="status">Filter by team status</param>
    /// <param name="tier">Filter by team tier</param>
    /// <param name="expiresOnFrom">Filter teams expiring after this date</param>
    /// <param name="expiresOnTo">Filter teams expiring before this date</param>
    /// <returns>Paginated list of teams</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<GlobalAdminTeamDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetTeams(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchQuery = null,
        [FromQuery] TeamStatus? status = null,
        [FromQuery] TeamTier? tier = null,
        [FromQuery] DateTime? expiresOnFrom = null,
        [FromQuery] DateTime? expiresOnTo = null)
    {
        try
        {
            var teams = await _globalAdminTeamService.GetTeamsAsync(
                pageNumber, pageSize, searchQuery, status, tier, expiresOnFrom, expiresOnTo);
            return Ok(teams);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve teams");
        }
    }

    /// <summary>
    /// Gets a paginated list of deleted teams that can be recovered.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by team name, subdomain, or owner email</param>
    /// <returns>Paginated list of deleted teams</returns>
    [HttpGet("deleted")]
    [ProducesResponseType(typeof(PaginatedList<DeletedTeamDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetDeletedTeams(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchQuery = null)
    {
        try
        {
            var deletedTeams = await _globalAdminTeamService.GetDeletedTeamsAsync(
                pageNumber, pageSize, searchQuery);
            return Ok(deletedTeams);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve deleted teams");
        }
    }

    /// <summary>
    /// Gets a team by ID (bypasses team access restrictions).
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Team details</returns>
    [HttpGet("{teamId:guid}")]
    [ProducesResponseType(typeof(GlobalAdminTeamDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTeamById(Guid teamId)
    {
        try
        {
            var team = await _globalAdminTeamService.GetTeamByIdAsync(teamId);
            return Ok(team);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to retrieve team {teamId}");
        }
    }

    /// <summary>
    /// Creates a new team with a new user as the owner.
    /// Creates both the user account and the team, then establishes the ownership relationship.
    /// </summary>
    /// <param name="dto">Team and owner creation data</param>
    /// <returns>Created team details</returns>
    [HttpPost("with-new-owner")]
    [ProducesResponseType(typeof(GlobalAdminTeamDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateTeamWithNewOwner([FromBody] CreateTeamWithNewOwnerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _globalAdminTeamService.CreateTeamWithNewOwnerAsync(dto);
            return CreatedAtAction(nameof(GetTeamById), new { teamId = team.Id }, team);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to create team with new owner");
        }
    }

    /// <summary>
    /// Creates a new team with an existing user as the owner.
    /// The user must exist and cannot already be the owner of another team.
    /// </summary>
    /// <param name="dto">Team creation data with existing owner</param>
    /// <returns>Created team details</returns>
    [HttpPost("with-existing-owner")]
    [ProducesResponseType(typeof(GlobalAdminTeamDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateTeamWithExistingOwner([FromBody] CreateTeamWithExistingOwnerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _globalAdminTeamService.CreateTeamWithExistingOwnerAsync(dto);
            return CreatedAtAction(nameof(GetTeamById), new { teamId = team.Id }, team);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to create team with existing owner");
        }
    }

    /// <summary>
    /// Updates a team's properties (all non-audit properties).
    /// This operation bypasses normal team access restrictions.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated team details</returns>
    [HttpPut("{teamId:guid}")]
    [ProducesResponseType(typeof(GlobalAdminTeamDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateTeam(Guid teamId, [FromBody] GlobalAdminUpdateTeamDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _globalAdminTeamService.UpdateTeamAsync(teamId, dto);
            return Ok(team);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to update team {teamId}");
        }
    }

    /// <summary>
    /// Soft deletes a team and all its associated data.
    /// The team can be recovered using the recover endpoint.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{teamId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteTeam(Guid teamId)
    {
        try
        {
            await _globalAdminTeamService.DeleteTeamAsync(teamId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to delete team {teamId}");
        }
    }

    /// <summary>
    /// Permanently removes a team and all its associated data.
    /// This operation cannot be undone.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{teamId:guid}/permanent")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PermanentlyDeleteTeam(Guid teamId)
    {
        try
        {
            await _globalAdminTeamService.PermanentlyDeleteTeamAsync(teamId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to permanently delete team {teamId}");
        }
    }

    /// <summary>
    /// Recovers a soft-deleted team and restores its active status.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>Recovered team details</returns>
    [HttpPost("{teamId:guid}/recover")]
    [ProducesResponseType(typeof(GlobalAdminTeamDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RecoverTeam(Guid teamId)
    {
        try
        {
            var team = await _globalAdminTeamService.RecoverTeamAsync(teamId);
            return Ok(team);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to recover team {teamId}");
        }
    }

    /// <summary>
    /// Initiates an immediate ownership transfer (bypasses normal transfer process).
    /// Updates both the Team.OwnerId and the UserTeam roles in a single transaction.
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Transfer data</param>
    /// <returns>Updated team details</returns>
    [HttpPost("{teamId:guid}/transfer-ownership")]
    [ProducesResponseType(typeof(GlobalAdminTeamDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TransferOwnership(Guid teamId, [FromBody] GlobalAdminTransferOwnershipDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _globalAdminTeamService.TransferOwnershipAsync(teamId, dto);
            return Ok(team);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to transfer ownership for team {teamId}");
        }
    }

    /// <summary>
    /// Validates that a subdomain is available for use.
    /// </summary>
    /// <param name="subdomain">Subdomain to check</param>
    /// <param name="excludeTeamId">Team ID to exclude from the check (for updates)</param>
    /// <returns>Availability status</returns>
    [HttpGet("subdomain-availability")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CheckSubdomainAvailability(
        [FromQuery] string subdomain,
        [FromQuery] Guid? excludeTeamId = null)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return BadRequest("Subdomain is required");
        }

        try
        {
            var isAvailable = await _globalAdminTeamService.IsSubdomainAvailableAsync(subdomain, excludeTeamId);
            return Ok(isAvailable);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to check subdomain availability");
        }
    }
} 