using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Users.Services;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<PaginatedList<UserManagementDto>> GetUsersAsync(int pageNumber, int pageSize)
    {
        var query = _userManager.Users.IgnoreQueryFilters()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new UserManagementDto
            {
                Id = u.Id,
                Email = u.Email!,
                DisplayName = $"{u.LastName}, {u.FirstName}",
                LockoutEnd = u.LockoutEnd.HasValue ? u.LockoutEnd.Value.DateTime : null,
                IsActive = u.IsActive,
                IsGlobalAdmin = u.IsGlobalAdmin,
                IsDeleted = u.IsDeleted
            });

        return await PaginatedList<UserManagementDto>.CreateAsync(query, pageNumber, pageSize);
    }

    public async Task SetGlobalAdminStatusAsync(Guid userId, bool isGlobalAdmin)
    {
        var user = await GetUserByIdAsync(userId);
        if (user.IsDeleted)
        {
            throw new InvalidOperationException("User is deleted and cannot be updated.");
        }
        user.SetGlobalAdmin(isGlobalAdmin);
        await _userManager.UpdateAsync(user);
    }

    public async Task RemoveLockoutAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        await _userManager.SetLockoutEndDateAsync(user, null);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user.IsDeleted)
        {
            throw new InvalidOperationException("User is already deleted.");
        }
        user.IsDeleted = true;
        user.DeletedOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task RestoreUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (!user.IsDeleted)
        {
                throw new InvalidOperationException("User is not deleted and cannot be restored.");
        }
        user.IsDeleted = false;
        user.DeletedOn = null;
        user.DeletedBy = null;
        await _userManager.UpdateAsync(user);
    }

    public async Task SetUserActiveStatusAsync(Guid userId, bool isActive)
    {
        var user = await GetUserByIdAsync(userId);
        if (user.IsDeleted)
        {
            throw new InvalidOperationException("User is deleted and cannot be updated.");
        }
        user.IsActive = isActive;
        user.ModifiedOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    private async Task<ApplicationUser> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }
        return user;
    }
} 