using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for team member management operations
/// </summary>
[Authorize]
public class TeamMembersController : BaseApiController
{
    private readonly ITeamManagementService _teamManagementService;

    public TeamMembersController(
        ITeamManagementService teamManagementService,
        ILogger<TeamMembersController> logger) : base(logger)
    {
        _teamManagementService = teamManagementService;
    }

    /// <summary>
    /// Gets a paginated list of team members
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="role">Filter by team role</param>
    /// <returns>Paginated list of team members</returns>
    [HttpGet("teams/{teamId:guid}/members")]
    [ProducesResponseType(typeof(PaginatedList<TeamMemberDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTeamMembers(
        Guid teamId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TeamRole? role = null)
    {
        try
        {
            var members = await _teamManagementService.GetTeamMembersAsync(teamId, pageNumber, pageSize, role);
            return Ok(members);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve team members");
        }
    }

    /// <summary>
    /// Updates a team member's role
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="newRole">New role for the member</param>
    /// <returns>Updated member details</returns>
    [HttpPut("teams/{teamId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(typeof(TeamMemberDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMemberRole(
        Guid teamId, 
        Guid userId, 
        [FromBody] TeamRole newRole)
    {
        try
        {
            var member = await _teamManagementService.UpdateMemberRoleAsync(teamId, userId, newRole);
            return Ok(member);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("not an active member"))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to update member role");
        }
    }

    /// <summary>
    /// Removes a member from the team
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>No content</returns>
    [HttpDelete("teams/{teamId:guid}/members/{userId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId)
    {
        try
        {
            await _teamManagementService.RemoveMemberAsync(teamId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("not an active member"))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to remove member");
        }
    }
} 