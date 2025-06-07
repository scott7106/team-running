using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamStride.Application.Common.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Services;

/// <summary>
/// Service for global admin dashboard operations.
/// </summary>
public class GlobalAdminDashboardService : IGlobalAdminDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;

    public GlobalAdminDashboardService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        _context = context;
        _userManager = userManager;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Gets dashboard statistics including active teams, total users, and global admins count.
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        await _authorizationService.RequireGlobalAdminAsync();

        // Get active teams count (not deleted and with Active status)
        var activeTeamsCount = await _context.Teams
            .IgnoreQueryFilters() // Bypass global query filters for global admin
            .CountAsync(t => !t.IsDeleted && t.Status == TeamStatus.Active);

        // Get total users count (not deleted)
        var totalUsersCount = await _context.Users
            .IgnoreQueryFilters() // Bypass global query filters for global admin
            .CountAsync(u => !u.IsDeleted);

        // Get global admins count (users with GlobalAdmin role)
        var globalAdminsCount = await _context.UserRoles
            .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "GlobalAdmin"))
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync();

        return new DashboardStatsDto
        {
            ActiveTeamsCount = activeTeamsCount,
            TotalUsersCount = totalUsersCount,
            GlobalAdminsCount = globalAdminsCount
        };
    }
} 