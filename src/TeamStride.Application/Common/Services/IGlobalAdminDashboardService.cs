namespace TeamStride.Application.Common.Services;

/// <summary>
/// Service interface for global admin dashboard operations.
/// </summary>
public interface IGlobalAdminDashboardService
{
    /// <summary>
    /// Gets dashboard statistics including active teams, total users, and global admins count.
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}

/// <summary>
/// DTO for dashboard statistics
/// </summary>
public class DashboardStatsDto
{
    public int ActiveTeamsCount { get; set; }
    public int TotalUsersCount { get; set; }
    public int GlobalAdminsCount { get; set; }
} 