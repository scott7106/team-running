using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Identity;

namespace TeamStride.Api.Controllers;

/// <summary>
/// Controller for public team operations that don't require authentication
/// </summary>
[ApiController]
[Route("api/public/teams")]
[AllowAnonymous]
public class PublicTeamsController : BaseApiController
{
    private readonly ISiteRegistrationService _siteRegistrationService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public PublicTeamsController(
        ISiteRegistrationService siteRegistrationService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<PublicTeamsController> logger) : base(logger)
    {
        _siteRegistrationService = siteRegistrationService;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Checks if a subdomain is available for team registration
    /// </summary>
    /// <param name="subdomain">Subdomain to check</param>
    /// <returns>Availability status</returns>
    [HttpGet("subdomain-availability")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckSubdomainAvailability([FromQuery] string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return BadRequest("Subdomain is required");
        }

        try
        {
            var isAvailable = await _siteRegistrationService.IsSubdomainAvailableAsync(subdomain);
            return Ok(isAvailable);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to check subdomain availability");
        }
    }

    /// <summary>
    /// Creates a new team with a new user as owner via public registration
    /// </summary>
    /// <param name="dto">Team and owner creation data</param>
    /// <returns>Team creation result with authentication info</returns>
    [HttpPost("with-new-owner")]
    [ProducesResponseType(typeof(PublicTeamCreationResultDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateTeamWithNewOwner([FromBody] CreateTeamWithNewOwnerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _siteRegistrationService.CreateTeamWithNewOwnerAsync(dto);
            
            // Authenticate the newly created user
            var user = await _userManager.FindByEmailAsync(dto.OwnerEmail);
            if (user != null)
            {
                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                return CreatedAtAction(nameof(CheckSubdomainAvailability), new { subdomain = result.TeamSubdomain }, result);
            }
            
            return StatusCode(500, "Team created but authentication failed");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to create team with new owner");
        }
    }
} 