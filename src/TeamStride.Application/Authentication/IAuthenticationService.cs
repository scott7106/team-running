using System.Threading.Tasks;
using TeamStride.Application.Authentication.Dtos;

namespace TeamStride.Application.Authentication;

public interface IAuthenticationService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> ConfirmEmailAsync(string userId, string token);
    Task<bool> SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(string userId, string token, string newPassword);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> LogoutAsync(string userId);

    // External authentication methods
    Task<AuthResponseDto> ExternalLoginAsync(ExternalAuthRequestDto request);
    Task<string> GetExternalLoginUrlAsync(string provider, string? tenantId = null);
    Task<string> GetExternalLoginCallbackUrlAsync(string provider);
} 