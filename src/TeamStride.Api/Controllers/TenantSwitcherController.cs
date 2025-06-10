using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Application.Authentication.Services;
using TeamStride.Application.Authentication.Dtos;
using TeamStride.Domain.Identity;

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
    private readonly ITeamStrideAuthenticationService _authenticationService;

    public TenantSwitcherController(
        ITenantSwitcherService tenantSwitcherService,
        ITeamStrideAuthenticationService authenticationService,
        ILogger<TenantSwitcherController> logger) : base(logger)
    {
        _tenantSwitcherService = tenantSwitcherService;
        _authenticationService = authenticationService;
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

    /// <summary>
    /// Gets all active tenants (teams) for public access - used by middleware for subdomain resolution
    /// </summary>
    /// <returns>List of all active team subdomains</returns>
    [HttpGet("{subdomain}/theme/")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SubdomainDto), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetThemeInfoBySubdomain(string subdomain)
    {
        try
        {
            var data = await _tenantSwitcherService.GetThemeInfoByDomainAsync(subdomain);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to retrieve theme data for subdomain");
        }
    }

    /// <summary>
    /// Switches to a specific tenant and generates a new JWT token with team context.
    /// Available for global admins and users with access to the specified team.
    /// </summary>
    /// <param name="teamId">The ID of the team to switch to</param>
    /// <returns>New authentication response with team-specific JWT token</returns>
    [HttpPost("switch/{teamId:guid}")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SwitchTenant(Guid teamId)
    {
        try
        {
            var response = await _authenticationService.LoginWithTeamAsync(teamId);
            return Ok(response);
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationException.ErrorCodes.TenantNotFound)
        {
            return NotFound(new { message = ex.Message, traceId = HttpContext.TraceIdentifier });
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationException.ErrorCodes.InvalidCredentials)
        {
            return Forbid(ex.Message);
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message, traceId = HttpContext.TraceIdentifier });
        }
        catch (Exception ex)
        {
            return HandleError(ex, $"Failed to switch to tenant {teamId}");
        }
    }
} 