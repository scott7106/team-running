using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TeamStride.Application.Authentication.Services;
using TeamStride.Application.Authentication.Dtos;

namespace TeamStride.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ITeamStrideAuthenticationService _authenticationService;

    public AuthenticationController(ITeamStrideAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authenticationService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authenticationService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
    {
        var result = await _authenticationService.RefreshTokenAsync(refreshToken);
        return Ok(result);
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        var result = await _authenticationService.ConfirmEmailAsync(userId, token);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> ForgotPassword([FromBody] string email)
    {
        var result = await _authenticationService.SendPasswordResetEmailAsync(email);
        return result ? Ok() : BadRequest("Failed to send password reset email");
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromQuery] Guid userId, [FromQuery] string token, [FromBody] string newPassword)
    {
        var result = await _authenticationService.ResetPasswordAsync(userId, token, newPassword);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userIdString = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authenticationService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return result ? Ok() : BadRequest("Password change failed");
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Logout()
    {
        var userIdString = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authenticationService.LogoutAsync(userId);
        return Ok();
    }

    [HttpGet("external-login/{provider}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> GetExternalLoginUrl(string provider, [FromQuery] string? teamId = null)
    {
        var url = await _authenticationService.GetExternalLoginUrlAsync(provider, teamId);
        return Ok(url);
    }

    [HttpGet("external-login/{provider}/callback")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> GetExternalLoginCallbackUrl(string provider)
    {
        var url = await _authenticationService.GetExternalLoginCallbackUrlAsync(provider);
        return Ok(url);
    }

    [HttpPost("external-login")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalAuthRequestDto request)
    {
        var result = await _authenticationService.ExternalLoginAsync(request);
        return Ok(result);
    }
}

public class ChangePasswordRequestDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
} 