using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Api.Authorization;
using TeamStride.Application.Common.Services;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Global admin dashboard controller for statistics and overview data.
/// All endpoints require global admin privileges.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
[RequireGlobalAdmin]
public class GlobalAdminDashboardController : BaseApiController
{
    private readonly IGlobalAdminDashboardService _dashboardService;

    public GlobalAdminDashboardController(
        IGlobalAdminDashboardService dashboardService,
        ILogger<GlobalAdminDashboardController> logger) : base(logger)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Gets dashboard statistics including total users, active teams, and global admins count.
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve dashboard statistics");
        }
    }
} 