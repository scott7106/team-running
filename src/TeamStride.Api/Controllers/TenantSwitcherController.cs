using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for tenant switching operations
/// </summary>
[ApiController]
[Route("api/tenant-switcher")]
[Authorize]
public class TenantSwitcherController : BaseApiController
{
    private readonly ITenantSwitcherService _tenantSwitcherService;

    public TenantSwitcherController(
        ITenantSwitcherService tenantSwitcherService,
        ILogger<TenantSwitcherController> logger) : base(logger)
    {
        _tenantSwitcherService = tenantSwitcherService;
    }

    /// <summary>
    /// Gets all tenants (teams) that the current user has access to for tenant switching
    /// </summary>
    /// <returns>List of available tenants for switching</returns>
    [HttpGet("tenants")]
    [ProducesResponseType(typeof(IEnumerable<TenantDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetUserTenants()
    {
        try
        {
            var tenants = await _tenantSwitcherService.GetUserTenantsAsync();
            return Ok(tenants);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message, traceId = HttpContext.TraceIdentifier });
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve user tenants");
        }
    }
} 