using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;

namespace TeamStride.Api.Controllers;

[ApiController]
[Route("api/teams/{teamId:guid}/registration")]
[Authorize]
public class TeamRegistrationController : ControllerBase
{
    private readonly ITeamRegistrationService _registrationService;
    private readonly ILogger<TeamRegistrationController> _logger;

    public TeamRegistrationController(
        ITeamRegistrationService registrationService,
        ILogger<TeamRegistrationController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    [HttpPost("windows")]
    public async Task<ActionResult<TeamRegistrationWindowDto>> CreateRegistrationWindow(
        Guid teamId,
        CreateRegistrationWindowDto dto)
    {
        try
        {
            var window = await _registrationService.CreateRegistrationWindowAsync(teamId, dto);
            return Ok(window);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("windows/{windowId:guid}")]
    public async Task<ActionResult<TeamRegistrationWindowDto>> UpdateRegistrationWindow(
        Guid teamId,
        Guid windowId,
        UpdateRegistrationWindowDto dto)
    {
        try
        {
            var window = await _registrationService.UpdateRegistrationWindowAsync(teamId, windowId, dto);
            return Ok(window);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("windows")]
    public async Task<ActionResult<List<TeamRegistrationWindowDto>>> GetRegistrationWindows(Guid teamId)
    {
        var windows = await _registrationService.GetRegistrationWindowsAsync(teamId);
        return Ok(windows);
    }

    [HttpGet("windows/active")]
    [AllowAnonymous]
    public async Task<ActionResult<TeamRegistrationWindowDto>> GetActiveRegistrationWindow(Guid teamId)
    {
        var window = await _registrationService.GetActiveRegistrationWindowAsync(teamId);
        if (window == null)
        {
            return NotFound("No active registration window found");
        }
        return Ok(window);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<TeamRegistrationDto>> SubmitRegistration(
        Guid teamId,
        SubmitRegistrationDto dto)
    {
        try
        {
            var registration = await _registrationService.SubmitRegistrationAsync(teamId, dto);
            return Ok(registration);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{registrationId:guid}/status")]
    public async Task<ActionResult<TeamRegistrationDto>> UpdateRegistrationStatus(
        Guid teamId,
        Guid registrationId,
        UpdateRegistrationStatusDto dto)
    {
        try
        {
            var registration = await _registrationService.UpdateRegistrationStatusAsync(teamId, registrationId, dto);
            return Ok(registration);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<TeamRegistrationDto>>> GetRegistrations(Guid teamId)
    {
        var registrations = await _registrationService.GetRegistrationsAsync(teamId);
        return Ok(registrations);
    }

    [HttpGet("waitlist")]
    public async Task<ActionResult<List<TeamRegistrationDto>>> GetWaitlist(Guid teamId)
    {
        var waitlist = await _registrationService.GetWaitlistAsync(teamId);
        return Ok(waitlist);
    }

    [HttpGet("validate-passcode")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> ValidatePasscode(
        Guid teamId,
        [FromQuery] string passcode)
    {
        var isValid = await _registrationService.ValidateRegistrationPasscodeAsync(teamId, passcode);
        return Ok(isValid);
    }

    [HttpGet("is-open")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> IsRegistrationWindowOpen(Guid teamId)
    {
        var isOpen = await _registrationService.IsRegistrationWindowOpenAsync(teamId);
        return Ok(isOpen);
    }

    [HttpGet("has-spots")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> HasAvailableSpots(Guid teamId)
    {
        var hasSpots = await _registrationService.HasAvailableSpotsAsync(teamId);
        return Ok(hasSpots);
    }
} 