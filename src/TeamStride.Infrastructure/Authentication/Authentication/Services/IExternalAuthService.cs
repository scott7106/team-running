using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Authentication.Authentication.Services;

public interface IExternalAuthService
{
    Task<ExternalUserInfo?> GetUserInfoAsync(string provider, string accessToken);
} 