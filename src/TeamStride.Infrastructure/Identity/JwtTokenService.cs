using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TeamStride.Application.Authentication.Services;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Entities;

namespace TeamStride.Infrastructure.Identity;

public interface IJwtTokenService
{
    Task<string> GenerateJwtTokenAsync(ApplicationUser user, Guid? teamId, TeamRole? teamRole, MemberType? memberType, string? teamSubdomain = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly AuthenticationConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(AuthenticationConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    public async Task<string> GenerateJwtTokenAsync(ApplicationUser user, Guid? teamId, TeamRole? teamRole, MemberType? memberType, string? teamSubdomain = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? throw new InvalidOperationException("User email is null")),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("first_name", user.FirstName),
            new("last_name", user.LastName)
        };

        if (teamId.HasValue)
        {
            claims.Add(new Claim("team_id", teamId.Value.ToString()));
        }

        if (teamRole.HasValue)
        {
            claims.Add(new Claim("team_role", teamRole.Value.ToString()));
        }

        if (memberType.HasValue)
        {
            claims.Add(new Claim("member_type", memberType.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(teamSubdomain))
        {
            claims.Add(new Claim("team_subdomain", teamSubdomain));
        }

        // Check if user has GlobalAdmin application role
        var userRoles = await _userManager.GetRolesAsync(user);
        var isGlobalAdmin = userRoles.Contains("GlobalAdmin");
        claims.Add(new Claim("is_global_admin", isGlobalAdmin.ToString().ToLowerInvariant()));

        // If user is global admin and no specific team context, set 'app' context
        if (isGlobalAdmin && !teamId.HasValue)
        {
            claims.Add(new Claim("team_subdomain", "app"));
            claims.Add(new Claim("team_role", "GlobalAdmin"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config.JwtIssuer,
            audience: _config.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.JwtExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JwtSecret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = _config.JwtAudience,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            if (!(validatedToken is JwtSecurityToken jwtSecurityToken) || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, 
                StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
} 