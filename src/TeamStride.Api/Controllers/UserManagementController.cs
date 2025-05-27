using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Users.Services;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Provides endpoints for global user management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "GlobalAdmin")]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Gets a paginated list of all users in the system
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UserManagementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<UserManagementDto>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var users = await _userManagementService.GetUsersAsync(pageNumber, pageSize);
        return Ok(users);
    }

    /// <summary>
    /// Sets a user's global admin status
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="isGlobalAdmin">Whether the user should be a global admin</param>
    [HttpPut("{userId:guid}/global-admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetGlobalAdminStatus(Guid userId, [FromBody] bool isGlobalAdmin)
    {
        try
        {
            await _userManagementService.SetGlobalAdminStatusAsync(userId, isGlobalAdmin);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {userId} not found.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Removes the lockout from a user account
    /// </summary>
    /// <param name="userId">The user's ID</param>
    [HttpPost("{userId:guid}/remove-lockout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLockout(Guid userId)
    {
        try
        {
            await _userManagementService.RemoveLockoutAsync(userId);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {userId} not found.");
        }
    }

    /// <summary>
    /// Soft deletes a user from the system
    /// </summary>
    /// <param name="userId">The user's ID</param>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            await _userManagementService.DeleteUserAsync(userId);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {userId} not found.");
        }
    }

    /// <summary>
    /// Restores a previously deleted user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    [HttpPost("{userId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreUser(Guid userId)
    {
        try
        {
            await _userManagementService.RestoreUserAsync(userId);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {userId} not found.");
        }
    }

    /// <summary>
    /// Sets a user's active status
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="isActive">Whether the user should be active</param>
    [HttpPut("{userId:guid}/active-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActiveStatus(Guid userId, [FromBody] bool isActive)
    {
        try
        {
            await _userManagementService.SetUserActiveStatusAsync(userId, isActive);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {userId} not found.");
        }
    }
} 