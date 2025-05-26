using TeamStride.Domain.Identity;

namespace TeamStride.Application.Authentication;

public interface IJwtTokenService
{
    string GenerateJwtToken(ApplicationUser user, string tenantId, string role);
    string GenerateRefreshToken();
} 