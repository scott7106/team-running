using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TeamStride.Application.Authentication;
using TeamStride.Application.Authentication.Dtos;

namespace TeamStride.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
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

    [HttpPost("confirm-email")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var result = await _authenticationService.ConfirmEmailAsync(userId, token);
        return result ? Ok() : BadRequest("Email confirmation failed");
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
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> ResetPassword([FromQuery] string userId, [FromQuery] string token, [FromBody] string newPassword)
    {
        var result = await _authenticationService.ResetPasswordAsync(userId, token, newPassword);
        return result ? Ok() : BadRequest("Password reset failed");
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
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
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _authenticationService.LogoutAsync(userId);
        return Ok();
    }

    [HttpGet("external-login/{provider}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> GetExternalLoginUrl(string provider, [FromQuery] string? tenantId = null)
    {
        var url = await _authenticationService.GetExternalLoginUrlAsync(provider, tenantId);
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