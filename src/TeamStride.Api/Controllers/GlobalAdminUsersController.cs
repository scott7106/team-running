using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Api.Authorization;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Users.Dtos;
using TeamStride.Application.Users.Services;
using TeamStride.Domain.Identity;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Global admin controller for user management operations.
/// All endpoints require global admin privileges and bypass normal user access restrictions.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize]
[RequireGlobalAdmin]
public class GlobalAdminUsersController : BaseApiController
{
    private readonly IGlobalAdminUserService _globalAdminUserService;

    public GlobalAdminUsersController(
        IGlobalAdminUserService globalAdminUserService,
        ILogger<GlobalAdminUsersController> logger) : base(logger)
    {
        _globalAdminUserService = globalAdminUserService;
    }

    /// <summary>
    /// Gets a paginated list of all users in the system with search and filtering capabilities.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by name, user id, or email</param>
    /// <param name="status">Filter by user status</param>
    /// <param name="isActive">Filter by active/inactive status</param>
    /// <param name="isDeleted">Filter by deleted status (false = active users, true = deleted users)</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<GlobalAdminUserDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchQuery = null,
        [FromQuery] UserStatus? status = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool isDeleted = false)
    {
        try
        {
            var users = await _globalAdminUserService.GetUsersAsync(
                pageNumber, pageSize, searchQuery, status, isActive, isDeleted);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve users");
        }
    }

    /// <summary>
    /// Gets a paginated list of deleted users that can be recovered.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by name, user id, or email</param>
    /// <returns>Paginated list of deleted users</returns>
    [HttpGet("deleted")]
    [ProducesResponseType(typeof(PaginatedList<DeletedUserDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetDeletedUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchQuery = null)
    {
        try
        {
            var deletedUsers = await _globalAdminUserService.GetDeletedUsersAsync(
                pageNumber, pageSize, searchQuery);
            return Ok(deletedUsers);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve deleted users");
        }
    }

    /// <summary>
    /// Gets a user by ID (bypasses user access restrictions).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        try
        {
            var user = await _globalAdminUserService.GetUserByIdAsync(userId);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to retrieve user {userId}");
        }
    }

    /// <summary>
    /// Creates a new user account with specified application and team roles.
    /// </summary>
    /// <param name="dto">User creation data</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateUser([FromBody] GlobalAdminCreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _globalAdminUserService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to create user");
        }
    }

    /// <summary>
    /// Updates a user's properties (profile information, roles, status).
    /// This operation bypasses normal user access restrictions.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated user details</returns>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] GlobalAdminUpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _globalAdminUserService.UpdateUserAsync(userId, dto);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to update user {userId}");
        }
    }

    /// <summary>
    /// Soft deletes a user account.
    /// The user can be recovered using RecoverUser endpoint.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            await _globalAdminUserService.DeleteUserAsync(userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to delete user {userId}");
        }
    }

    /// <summary>
    /// Permanently removes a user account and all associated data.
    /// This operation cannot be undone.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{userId:guid}/purge")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PurgeUser(Guid userId)
    {
        try
        {
            await _globalAdminUserService.PurgeUserAsync(userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to purge user {userId}");
        }
    }

    /// <summary>
    /// Recovers a soft-deleted user account and restores its active status.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Recovered user details</returns>
    [HttpPost("{userId:guid}/recover")]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RecoverUser(Guid userId)
    {
        try
        {
            var user = await _globalAdminUserService.RecoverUserAsync(userId);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to recover user {userId}");
        }
    }

    /// <summary>
    /// Resets the lockout status for a user account, immediately unlocking the account.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Updated user details</returns>
    [HttpPost("{userId:guid}/reset-lockout")]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResetLockout(Guid userId)
    {
        try
        {
            var user = await _globalAdminUserService.ResetLockoutAsync(userId);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to reset lockout for user {userId}");
        }
    }

    /// <summary>
    /// Resets a user's password to a new temporary password.
    /// The user will be required to change the password on next login.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Password reset data</param>
    /// <returns>Password reset result with temporary password</returns>
    [HttpPost("{userId:guid}/reset-password")]
    [ProducesResponseType(typeof(PasswordResetResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] GlobalAdminResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _globalAdminUserService.ResetPasswordAsync(userId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to reset password for user {userId}");
        }
    }

    /// <summary>
    /// Validates that an email address is available for use.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <param name="excludeUserId">User ID to exclude from the check (for updates)</param>
    /// <returns>True if available, false if taken</returns>
    [HttpGet("email-availability")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CheckEmailAvailability(
        [FromQuery] string email,
        [FromQuery] Guid? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required");
        }

        try
        {
            var isAvailable = await _globalAdminUserService.IsEmailAvailableAsync(email, excludeUserId);
            return Ok(isAvailable);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to check email availability");
        }
    }

    #region Application Role Management

    /// <summary>
    /// Gets all available application roles in the system.
    /// </summary>
    /// <returns>List of application roles</returns>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(List<ApplicationRoleDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetApplicationRoles()
    {
        try
        {
            var roles = await _globalAdminUserService.GetApplicationRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve application roles");
        }
    }

    /// <summary>
    /// Gets the current application roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of role names assigned to the user</returns>
    [HttpGet("{userId:guid}/roles")]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserApplicationRoles(Guid userId)
    {
        try
        {
            var roles = await _globalAdminUserService.GetUserApplicationRolesAsync(userId);
            return Ok(roles);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to retrieve roles for user {userId}");
        }
    }

    /// <summary>
    /// Adds an application role to a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to add</param>
    /// <returns>Updated user details</returns>
    [HttpPost("{userId:guid}/roles/{roleName}")]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddUserToRole(Guid userId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest("Role name is required");
        }

        try
        {
            var user = await _globalAdminUserService.AddUserToRoleAsync(userId, roleName);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to add user {userId} to role {roleName}");
        }
    }

    /// <summary>
    /// Removes an application role from a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to remove</param>
    /// <returns>Updated user details</returns>
    [HttpDelete("{userId:guid}/roles/{roleName}")]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveUserFromRole(Guid userId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest("Role name is required");
        }

        try
        {
            var user = await _globalAdminUserService.RemoveUserFromRoleAsync(userId, roleName);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to remove user {userId} from role {roleName}");
        }
    }

    /// <summary>
    /// Sets all application roles for a user, replacing any existing role assignments.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Role assignment data</param>
    /// <returns>Updated user details</returns>
    [HttpPut("{userId:guid}/roles")]
    [ProducesResponseType(typeof(GlobalAdminUserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetUserRoles(Guid userId, [FromBody] SetUserRolesDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _globalAdminUserService.SetUserRolesAsync(userId, dto);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to set roles for user {userId}");
        }
    }

    /// <summary>
    /// Validates that a role name exists in the system.
    /// </summary>
    /// <param name="roleName">Role name to validate</param>
    /// <returns>True if role exists, false otherwise</returns>
    [HttpGet("roles/{roleName}/exists")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CheckRoleExists(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest("Role name is required");
        }

        try
        {
            var exists = await _globalAdminUserService.RoleExistsAsync(roleName);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to check if role {roleName} exists");
        }
    }

    #endregion
} 