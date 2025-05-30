using TeamStride.Application.Common.Models;
using TeamStride.Application.Users.Dtos;
using TeamStride.Domain.Identity;

namespace TeamStride.Application.Users.Services;

/// <summary>
/// Service interface for global admin user management operations.
/// These operations bypass normal user access restrictions and are only available to global admins.
/// </summary>
public interface IGlobalAdminUserService
{
    /// <summary>
    /// Gets a paginated list of all users in the system with search and filtering capabilities.
    /// Global query filters are disabled for this operation.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by name, user id, or email</param>
    /// <param name="status">Filter by user status</param>
    /// <param name="isActive">Filter by active/inactive status</param>
    /// <param name="isDeleted">Filter by deleted status (false = active users, true = deleted users)</param>
    /// <returns>Paginated list of users</returns>
    Task<PaginatedList<GlobalAdminUserDto>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null,
        UserStatus? status = null,
        bool? isActive = null,
        bool isDeleted = false);

    /// <summary>
    /// Gets a paginated list of deleted users that can be recovered.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchQuery">Search by name, user id, or email</param>
    /// <returns>Paginated list of deleted users</returns>
    Task<PaginatedList<DeletedUserDto>> GetDeletedUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null);

    /// <summary>
    /// Gets a user by ID (bypasses user access restrictions).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User details</returns>
    Task<GlobalAdminUserDto> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Creates a new user account with specified application and team roles.
    /// </summary>
    /// <param name="dto">User creation data</param>
    /// <returns>Created user details</returns>
    Task<GlobalAdminUserDto> CreateUserAsync(GlobalAdminCreateUserDto dto);

    /// <summary>
    /// Updates a user's properties (profile information, roles, status).
    /// This operation bypasses normal user access restrictions.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated user details</returns>
    Task<GlobalAdminUserDto> UpdateUserAsync(Guid userId, GlobalAdminUpdateUserDto dto);

    /// <summary>
    /// Soft deletes a user account.
    /// The user can be recovered using RecoverUserAsync.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteUserAsync(Guid userId);

    /// <summary>
    /// Permanently removes a user account and all associated data.
    /// This operation cannot be undone.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task PermanentlyDeleteUserAsync(Guid userId);

    /// <summary>
    /// Recovers a soft-deleted user account and restores its active status.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Recovered user details</returns>
    Task<GlobalAdminUserDto> RecoverUserAsync(Guid userId);

    /// <summary>
    /// Resets the lockout status for a user account, immediately unlocking the account.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Updated user details</returns>
    Task<GlobalAdminUserDto> ResetLockoutAsync(Guid userId);

    /// <summary>
    /// Resets a user's password to a new temporary password.
    /// The user will be required to change the password on next login.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Password reset data</param>
    /// <returns>Password reset result with temporary password</returns>
    Task<PasswordResetResultDto> ResetPasswordAsync(Guid userId, GlobalAdminResetPasswordDto dto);

    /// <summary>
    /// Validates that an email address is available for use.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <param name="excludeUserId">User ID to exclude from the check (for updates)</param>
    /// <returns>True if available, false if taken</returns>
    Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null);

    // Application Role Management Methods

    /// <summary>
    /// Gets all available application roles in the system.
    /// </summary>
    /// <returns>List of application roles</returns>
    Task<List<ApplicationRoleDto>> GetApplicationRolesAsync();

    /// <summary>
    /// Gets the current application roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of role names assigned to the user</returns>
    Task<List<string>> GetUserApplicationRolesAsync(Guid userId);

    /// <summary>
    /// Adds an application role to a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to add</param>
    /// <returns>Updated user details</returns>
    Task<GlobalAdminUserDto> AddUserToRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Removes an application role from a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to remove</param>
    /// <returns>Updated user details</returns>
    Task<GlobalAdminUserDto> RemoveUserFromRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Sets all application roles for a user, replacing any existing role assignments.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Role assignment data</param>
    /// <returns>Updated user details</returns>
    Task<GlobalAdminUserDto> SetUserRolesAsync(Guid userId, SetUserRolesDto dto);

    /// <summary>
    /// Validates that a role name exists in the system.
    /// </summary>
    /// <param name="roleName">Role name to validate</param>
    /// <returns>True if role exists, false otherwise</returns>
    Task<bool> RoleExistsAsync(string roleName);
} 