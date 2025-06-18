using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Identity;
using TeamStride.Application.Authentication.Services;
using System.Web;

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
    private readonly ITeamStrideAuthenticationService _authenticationService;

    public PublicTeamsController(
        ISiteRegistrationService siteRegistrationService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ITeamStrideAuthenticationService authenticationService,
        ILogger<PublicTeamsController> logger) : base(logger)
    {
        _siteRegistrationService = siteRegistrationService;
        _signInManager = signInManager;
        _userManager = userManager;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Checks if a subdomain is available for team registration
    /// </summary>
    /// <param name="subdomain">Subdomain to check</param>
    /// <param name="excludeTeamId">Optional team ID to exclude from the check (for editing existing teams)</param>
    /// <returns>Availability status</returns>
    [HttpGet("subdomain-availability")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckSubdomainAvailability([FromQuery] string subdomain, [FromQuery] string? excludeTeamId = null)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return BadRequest("Subdomain is required");
        }

        Guid? excludeTeamGuid = null;
        if (!string.IsNullOrEmpty(excludeTeamId) && Guid.TryParse(excludeTeamId, out var parsedGuid))
        {
            excludeTeamGuid = parsedGuid;
        }

        try
        {
            var isAvailable = await _siteRegistrationService.IsSubdomainAvailableAsync(subdomain, excludeTeamGuid);
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
            
            // Authenticate the newly created user and generate tokens for silent login
            var user = await _userManager.FindByEmailAsync(dto.OwnerEmail);
            if (user != null)
            {
                // Sign in the user for regular session
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                // Perform a proper login to get tokens with proper refresh token management
                var loginRequest = new Application.Authentication.Dtos.LoginRequestDto
                {
                    Email = user.Email!,
                    Password = dto.OwnerPassword // Use the password from the registration
                };
                
                var authResponse = await _authenticationService.LoginAsync(loginRequest);
                
                // Modify the redirect URL to include authentication tokens
                var redirectUrl = result.RedirectUrl;
                var uriBuilder = new UriBuilder(redirectUrl);
                var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                query["token"] = authResponse.Token;
                query["refreshToken"] = authResponse.RefreshToken;
                uriBuilder.Query = query.ToString();
                
                // Update the result with the modified redirect URL
                result.RedirectUrl = uriBuilder.ToString();
                
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

    /// <summary>
    /// Creates a new team with an existing authenticated user as owner via public registration
    /// </summary>
    /// <param name="dto">Team creation data</param>
    /// <returns>Team creation result with redirect info</returns>
    [HttpPost("with-existing-owner")]
    [Authorize] // Requires authentication
    [ProducesResponseType(typeof(PublicTeamCreationResultDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateTeamWithExistingOwner([FromBody] CreateTeamWithExistingOwnerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get current user ID from claims
        var userIdString = User.FindFirst("sub")?.Value ?? 
                           User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
        {
            return Unauthorized("Unable to determine current user");
        }

        try
        {
            var result = await _siteRegistrationService.CreateTeamWithExistingOwnerAsync(dto, currentUserId);
            
            return CreatedAtAction(nameof(CheckSubdomainAvailability), new { subdomain = result.TeamSubdomain }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Failed to create team with existing owner");
        }
    }
} 