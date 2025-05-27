using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for team subscription and branding management
/// </summary>
[Authorize]
public class TeamSubscriptionController : BaseApiController
{
    private readonly ITeamManagementService _teamManagementService;

    public TeamSubscriptionController(
        ITeamManagementService teamManagementService,
        ILogger<TeamSubscriptionController> logger) : base(logger)
    {
        _teamManagementService = teamManagementService;
    }

    /// <summary>
    /// Updates a team's subscription tier and expiration
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Subscription update data</param>
    /// <returns>Updated team details</returns>
    [HttpPut("teams/{teamId:guid}/subscription")]
    [ProducesResponseType(typeof(TeamManagementDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSubscription(Guid teamId, [FromBody] UpdateSubscriptionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _teamManagementService.UpdateSubscriptionAsync(teamId, dto);
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
            return HandleError(ex, "Failed to update subscription");
        }
    }

    /// <summary>
    /// Updates a team's branding (colors and logo)
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Branding update data</param>
    /// <returns>Updated team details</returns>
    [HttpPut("teams/{teamId:guid}/branding")]
    [ProducesResponseType(typeof(TeamManagementDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateBranding(Guid teamId, [FromBody] UpdateTeamBrandingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var team = await _teamManagementService.UpdateBrandingAsync(teamId, dto);
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
            return HandleError(ex, "Failed to update branding");
        }
    }
} 