using TeamStride.Domain.Identity;

namespace TeamStride.Application.Authentication.Services;

public interface IExternalAuthService
{
    Task<ExternalUserInfo?> GetUserInfoAsync(string provider, string accessToken);
} 