using TeamStride.Application.Teams.Dtos;

namespace TeamStride.Application.Teams.Services;

public interface ITeamRegistrationService
{
    Task<TeamRegistrationWindowDto> CreateRegistrationWindowAsync(Guid teamId, CreateRegistrationWindowDto dto);
    Task<TeamRegistrationWindowDto> UpdateRegistrationWindowAsync(Guid teamId, Guid windowId, UpdateRegistrationWindowDto dto);
    Task<List<TeamRegistrationWindowDto>> GetRegistrationWindowsAsync(Guid teamId);
    Task<TeamRegistrationWindowDto> GetActiveRegistrationWindowAsync(Guid teamId);
    Task<TeamRegistrationDto> SubmitRegistrationAsync(Guid teamId, SubmitRegistrationDto dto);
    Task<TeamRegistrationDto> UpdateRegistrationStatusAsync(Guid teamId, Guid registrationId, UpdateRegistrationStatusDto dto);
    Task<List<TeamRegistrationDto>> GetRegistrationsAsync(Guid teamId);
    Task<List<TeamRegistrationDto>> GetWaitlistAsync(Guid teamId);
    // Validation methods
    Task<bool> ValidateRegistrationPasscodeAsync(Guid teamId, string passcode);
    Task<bool> IsRegistrationWindowOpenAsync(Guid teamId);
    Task<bool> HasAvailableSpotsAsync(Guid teamId);
} 