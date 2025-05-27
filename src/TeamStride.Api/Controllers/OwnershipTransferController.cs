using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for team ownership transfer operations
/// </summary>
[Authorize]
public class OwnershipTransferController : BaseApiController
{
    private readonly ITeamManagementService _teamManagementService;

    public OwnershipTransferController(
        ITeamManagementService teamManagementService,
        ILogger<OwnershipTransferController> logger) : base(logger)
    {
        _teamManagementService = teamManagementService;
    }

    /// <summary>
    /// Initiates an ownership transfer for a team
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <param name="dto">Transfer details</param>
    /// <returns>Transfer details</returns>
    [HttpPost("teams/{teamId:guid}/transfer")]
    [ProducesResponseType(typeof(OwnershipTransferDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> InitiateOwnershipTransfer(Guid teamId, [FromBody] TransferOwnershipDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var transfer = await _teamManagementService.InitiateOwnershipTransferAsync(teamId, dto);
            return CreatedAtAction(nameof(GetPendingTransfers), new { teamId }, transfer);
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
            return HandleError(ex, "Failed to initiate ownership transfer");
        }
    }

    /// <summary>
    /// Completes an ownership transfer using a transfer token
    /// </summary>
    /// <param name="transferToken">Transfer token</param>
    /// <returns>Updated team details</returns>
    [HttpPost("complete/{transferToken}")]
    [AllowAnonymous] // Allow anonymous access for completing transfers via email link
    [ProducesResponseType(typeof(TeamManagementDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CompleteOwnershipTransfer(string transferToken)
    {
        if (string.IsNullOrWhiteSpace(transferToken))
        {
            return BadRequest("Transfer token is required");
        }

        try
        {
            var team = await _teamManagementService.CompleteOwnershipTransferAsync(transferToken);
            return Ok(team);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid transfer token"))
        {
            return NotFound("Transfer token not found or invalid");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to complete ownership transfer");
        }
    }

    /// <summary>
    /// Cancels a pending ownership transfer
    /// </summary>
    /// <param name="transferId">Transfer ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{transferId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CancelOwnershipTransfer(Guid transferId)
    {
        try
        {
            await _teamManagementService.CancelOwnershipTransferAsync(transferId);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to cancel ownership transfer");
        }
    }

    /// <summary>
    /// Gets pending ownership transfers for a team
    /// </summary>
    /// <param name="teamId">Team ID</param>
    /// <returns>List of pending transfers</returns>
    [HttpGet("teams/{teamId:guid}/pending")]
    [ProducesResponseType(typeof(IEnumerable<OwnershipTransferDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPendingTransfers(Guid teamId)
    {
        try
        {
            var transfers = await _teamManagementService.GetPendingTransfersAsync(teamId);
            return Ok(transfers);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve pending transfers");
        }
    }
} 