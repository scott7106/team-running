using TeamStride.Application.Common.Models;
using TeamStride.Application.Users.Dtos;

namespace TeamStride.Application.Users.Services;

public interface IUserManagementService
{
    /// <summary>
    /// Gets a paginated list of all users in the system, sorted by display name (LastName, FirstName)
    /// Includes both active and deleted users
    /// </summary>
    Task<PaginatedList<UserManagementDto>> GetUsersAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Removes the lockout from a user account
    /// </summary>
    Task RemoveLockoutAsync(Guid userId);

    /// <summary>
    /// Soft deletes a user from the system
    /// </summary>
    Task DeleteUserAsync(Guid userId);

    /// <summary>
    /// Restores a previously deleted user
    /// </summary>
    Task RestoreUserAsync(Guid userId);

    /// <summary>
    /// Sets a user's active status
    /// </summary>
    Task SetUserActiveStatusAsync(Guid userId, bool isActive);
}

public class UserManagementDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;  // Formatted as "LastName, FirstName"
    public DateTime? LockoutEnd { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
} 