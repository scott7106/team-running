using TeamStride.Application.Authentication.Dtos;

namespace TeamStride.Application.Authentication.Services;

public interface ITeamStrideAuthenticationService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> ConfirmEmailAsync(Guid userId, string token);
    Task<bool> SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<bool> LogoutAsync(Guid userId);
    Task<string> GetExternalLoginUrlAsync(string provider, string? tenantId = null);
    Task<string> GetExternalLoginCallbackUrlAsync(string provider);
    Task<AuthResponseDto> ExternalLoginAsync(ExternalAuthRequestDto request);
} 