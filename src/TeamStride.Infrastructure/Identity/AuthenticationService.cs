using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using TeamStride.Application.Authentication.Services;
using TeamStride.Application.Authentication.Dtos;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Entities;
using TeamStride.Infrastructure.Email;
using TeamStride.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Common.Services;

namespace TeamStride.Infrastructure.Identity;

public class AuthenticationService : ITeamStrideAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IExternalAuthService _externalAuthService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        ApplicationDbContext context,
        ILogger<AuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IExternalAuthService externalAuthService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _externalAuthService = externalAuthService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Email);
        ArgumentNullException.ThrowIfNull(request.Password);

        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null)
        {
            throw new AuthenticationException("Invalid credentials", AuthenticationException.ErrorCodes.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            throw new AuthenticationException("Account is locked", AuthenticationException.ErrorCodes.AccountLocked);
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AuthenticationException("Invalid credentials", AuthenticationException.ErrorCodes.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
        {
            throw new AuthenticationException("Email not confirmed", AuthenticationException.ErrorCodes.EmailNotConfirmed);
        }

        // Check if user is a global admin
        var userRoles = await _userManager.GetRolesAsync(user);
        var isGlobalAdmin = userRoles.Contains("GlobalAdmin");
        
        // Get all active team memberships for the user
        var teams = await GetAllUserTeamMemberships(user.Id);

        
        // Update last login
        user.LastLoginOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);
        var refreshToken = await CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsGlobalAdmin = isGlobalAdmin,
            Teams = teams,
            RequiresEmailConfirmation = false
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Email);
        ArgumentNullException.ThrowIfNull(request.Password);
        ArgumentNullException.ThrowIfNull(request.TeamId);
        ArgumentNullException.ThrowIfNull(request.Role);

        // Validate team
        var team = await _context.Teams.FindAsync(request.TeamId) ??
            throw new AuthenticationException("Invalid team", AuthenticationException.ErrorCodes.TenantNotFound);

        // Check if email exists
        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            throw new AuthenticationException("Email already registered", AuthenticationException.ErrorCodes.EmailAlreadyExists);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DefaultTeamId = request.TeamId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException($"Registration failed: {errors}");
        }

        UserTeam? userTeam = null;
        if (request.TeamId.HasValue) 
        {
            // Create user-team relationship
            userTeam = new UserTeam
            {
                UserId = user.Id,
                TeamId = request.TeamId.Value,
                Role = request.Role,
                IsActive = true,
                IsDefault = true,
                JoinedOn = DateTime.UtcNow
            };
            _context.UserTeams.Add(userTeam);
            await _context.SaveChangesAsync();
        }        

        // Generate email confirmation token and send email
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var baseUrl = _configuration["Authentication:BaseUrl"] ?? "https://teamstride.com";
        var confirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);

        // Generate tokens
        var teams = request.TeamId.HasValue ?  
            new List<TeamMembershipDto>
            {
                new()
                {
                    TeamId = request.TeamId.Value,
                    TeamSubdomain = team.Subdomain,
                    TeamRole = request.Role,
                    MemberType = userTeam?.MemberType ?? MemberType.Athlete
                }
            }
            : new List<TeamMembershipDto>();
        
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);
        var refreshToken = await CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsGlobalAdmin = false,
            Teams = teams,
            RequiresEmailConfirmation = true
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken) ??
            throw new AuthenticationException("Invalid refresh token", AuthenticationException.ErrorCodes.InvalidToken);

        if (!token.IsActive)
        {
            throw new AuthenticationException("Refresh token is expired or revoked", AuthenticationException.ErrorCodes.InvalidToken);
        }

        // Check if user is a global admin
        var userRoles = await _userManager.GetRolesAsync(token.User!);
        var isGlobalAdmin = userRoles.Contains("GlobalAdmin");
        
        // Get all active team memberships for the user
        var teams = await GetAllUserTeamMemberships(token.UserId);

        // Generate new tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(token.User!, teams);
        var newRefreshToken = await CreateRefreshTokenAsync(token.User!);

        // Revoke old refresh token
        token.RevokedOn = DateTime.UtcNow;
        token.ReplacedByToken = newRefreshToken.Token;
        token.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = newRefreshToken.Token,
            Email = token.User!.Email ?? string.Empty,
            FirstName = token.User.FirstName,
            LastName = token.User.LastName,
            IsGlobalAdmin = isGlobalAdmin,
            Teams = teams,
            RequiresEmailConfirmation = false
        };
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? 
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException($"Email confirmation failed: {errors}");
        }

        return true;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email) ?? 
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var baseUrl = _configuration["Authentication:BaseUrl"] ?? "https://teamstride.com";
        var resetLink = $"{baseUrl}/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetAsync(email, resetLink);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? 
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException($"Password reset failed: {errors}");
        }

        // Revoke all refresh tokens
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedOn == null && rt.ExpiresOn > DateTime.UtcNow)
            .ToListAsync();

        foreach (var refreshToken in tokens)
        {
            refreshToken.RevokedOn = DateTime.UtcNow;
            refreshToken.ReasonRevoked = "Password reset";
            refreshToken.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException($"Password change failed: {errors}");
        }

        // Revoke all refresh tokens
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedOn == null && rt.ExpiresOn > DateTime.UtcNow)
            .ToListAsync();

        foreach (var refreshToken in tokens)
        {
            refreshToken.RevokedOn = DateTime.UtcNow;
            refreshToken.ReasonRevoked = "Password changed";
            refreshToken.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        // Revoke all active refresh tokens
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedOn == null && rt.ExpiresOn > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedOn = DateTime.UtcNow;
            token.ReasonRevoked = "Logged out";
            token.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResponseDto> ExternalLoginAsync(ExternalAuthRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Provider);
        ArgumentNullException.ThrowIfNull(request.AccessToken);
        ArgumentNullException.ThrowIfNull(request.TeamId);

        var externalUser = await _externalAuthService.GetUserInfoAsync(request.Provider, request.AccessToken);

        if (externalUser == null)
        {
            throw new AuthenticationException("Failed to get external user info", AuthenticationException.ErrorCodes.ExternalAuthError);
        }

        var user = await _userManager.FindByEmailAsync(externalUser.Email);
        var isNewUser = user == null;

        if (isNewUser)
        {
            user = new ApplicationUser
            {
                UserName = externalUser.Email,
                Email = externalUser.Email,
                FirstName = externalUser.FirstName,
                LastName = externalUser.LastName,
                EmailConfirmed = true,
                IsActive = true,
                DefaultTeamId = request.TeamId
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AuthenticationException($"External registration failed: {errors}");
            }

            UserTeam? newUserTeam = null;
            if (request.TeamId.HasValue)
            {
                // Create user-team relationship with default role
                newUserTeam = new UserTeam
                {
                    UserId = user.Id,
                    TeamId = request.TeamId.Value,
                    Role = TeamRole.TeamMember, // Default role for external users
                    IsActive = true
                };
                _context.UserTeams.Add(newUserTeam);
                await _context.SaveChangesAsync();
            }            
        }

        // Check if user is a global admin
        var userRoles = await _userManager.GetRolesAsync(user!);
        var isGlobalAdmin = userRoles.Contains("GlobalAdmin");
        
        // Get all active team memberships for the user
        var teams = new List<TeamMembershipDto>();
        var userTeams = await _context.UserTeams
            .Include(ut => ut.Team)
            .Where(ut => ut.UserId == user!.Id && ut.IsActive)
            .ToListAsync();
            
        teams = userTeams.Select(ut => new TeamMembershipDto
        {
            TeamId = ut.TeamId,
            TeamSubdomain = ut.Team!.Subdomain,
            TeamRole = ut.Role,
            MemberType = ut.MemberType
        }).ToList();
        

        // Update last login
        user!.LastLoginOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);
        var refreshToken = await CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsGlobalAdmin = isGlobalAdmin,
            Teams = teams,
            RequiresEmailConfirmation = false
        };
    }

    public async Task<string> GetExternalLoginUrlAsync(string provider, string? teamId = null)
    {
        var clientId = provider.ToLowerInvariant() switch
        {
            "google" => _configuration["Authentication:Google:ClientId"],
            "facebook" => _configuration["Authentication:Facebook:ClientId"],
            "microsoft" => _configuration["Authentication:Microsoft:ClientId"],
            "twitter" => _configuration["Authentication:Twitter:ClientId"],
            _ => throw new AuthenticationException($"Unsupported provider: {provider}")
        };

        if (string.IsNullOrEmpty(clientId))
        {
            throw new AuthenticationException($"Client ID not configured for provider: {provider}");
        }

        var redirectUri = await GetExternalLoginCallbackUrlAsync(provider);
        var state = teamId ?? string.Empty;

        return provider.ToLowerInvariant() switch
        {
            "google" => $"https://accounts.google.com/o/oauth2/v2/auth" +
                       $"?client_id={clientId}" +
                       $"&response_type=code" +
                       $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                       $"&scope=openid email profile" +
                       $"&state={state}",

            "facebook" => $"https://www.facebook.com/v12.0/dialog/oauth" +
                         $"?client_id={clientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&scope=email public_profile" +
                         $"&state={state}",

            "microsoft" => $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize" +
                         $"?client_id={clientId}" +
                         $"&response_type=code" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&scope=openid email profile" +
                         $"&state={state}",

            "twitter" => $"https://twitter.com/i/oauth2/authorize" +
                       $"?client_id={clientId}" +
                       $"&response_type=code" +
                       $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                       $"&scope=users.read tweet.read" +
                       $"&state={state}",

            _ => throw new AuthenticationException($"Unsupported provider: {provider}")
        };
    }

    public async Task<string> GetExternalLoginCallbackUrlAsync(string provider)
    {
        var baseUrl = _configuration["Authentication:ExternalProviders:BaseUrl"] ?? 
            throw new InvalidOperationException("Authentication:ExternalProviders:BaseUrl not configured");
        return await Task.FromResult($"{baseUrl}/api/authentication/external-login/{provider.ToLowerInvariant()}/callback");
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user)
    {
        var refreshToken = new RefreshToken
        {
            Token = _jwtTokenService.GenerateRefreshToken(),
            UserId = user.Id,
            TeamId = Guid.Empty, // No longer team-specific
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private async Task<List<TeamMembershipDto>> GetAllUserTeamMemberships(Guid userId)
    {
        // Get user's actual team memberships regardless of context
        var userTeams = await _context.UserTeams
            .Include(ut => ut.Team)
            .Where(ut => ut.UserId == userId && ut.IsActive)
            .ToListAsync();
            
        return userTeams.Select(ut => new TeamMembershipDto
        {
            TeamId = ut.TeamId,
            TeamSubdomain = ut.Team!.Subdomain,
            TeamRole = ut.Role,
            MemberType = ut.MemberType
        }).ToList();
    }

    public async Task<bool> ValidateHeartbeatAsync(Guid userId, string fingerprint)
    {
        _logger.LogDebug("Starting heartbeat validation for user {UserId}", userId);
        
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Heartbeat validation failed: User {UserId} not found", userId);
            return false;
        }
        
        if (!user.IsActive)
        {
            _logger.LogWarning("Heartbeat validation failed: User {UserId} is not active", userId);
            return false;
        }
        
        _logger.LogDebug("User {UserId} found and active. ForceLogoutAfter: {ForceLogoutAfter}", userId, user.ForceLogoutAfter);
        
        // Check if user has been force-logged out
        if (user.ForceLogoutAfter.HasValue && user.ForceLogoutAfter < DateTime.UtcNow)
        {
            _logger.LogWarning("Heartbeat validation failed: User {UserId} was force-logged out at {ForceLogoutAfter}", userId, user.ForceLogoutAfter);
            return false;
        }
        
        _logger.LogDebug("Updating last activity for user {UserId}", userId);
        
        // Update last activity
        user.LastActivityOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        _logger.LogDebug("Validating fingerprint for user {UserId}", userId);
        
        // Optional: Store/validate fingerprint
        await ValidateOrStoreFingerprint(userId, fingerprint);
        
        _logger.LogDebug("Heartbeat validation successful for user {UserId}", userId);
        return true;
    }

    private async Task ValidateOrStoreFingerprint(Guid userId, string fingerprint)
    {
        var existingFingerprint = await _context.UserSessions
            .Where(us => us.UserId == userId && us.IsActive)
            .Select(us => us.Fingerprint)
            .FirstOrDefaultAsync();
        
        if (existingFingerprint == null)
        {
            // First time - store fingerprint
            var session = new UserSession
            {
                UserId = userId,
                Fingerprint = fingerprint,
                CreatedOn = DateTime.UtcNow,
                LastActiveOn = DateTime.UtcNow,
                IsActive = true
            };
            
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
        }
        else if (existingFingerprint != fingerprint)
        {
            // Fingerprint mismatch - potential security issue
            _logger.LogWarning("Fingerprint mismatch for user {UserId}. Stored: {Stored}, Current: {Current}", 
                userId, existingFingerprint, fingerprint);
            
            // You could choose to invalidate the session here
            // For now, we'll log and continue, but you might want to return false
        }
    }

    public async Task<bool> ForceUserLogoutAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;
        
        user.ForceLogoutAfter = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        // Revoke all refresh tokens
        await LogoutAsync(userId);
        
        return true;
    }

    public async Task<AuthResponseDto> LoginWithTeamAsync(Guid teamId)
    {
        // Get current user from HTTP context
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new AuthenticationException("User not authenticated", AuthenticationException.ErrorCodes.InvalidCredentials);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString()) ??
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);

        if (!user.IsActive)
        {
            throw new AuthenticationException("Account is locked", AuthenticationException.ErrorCodes.AccountLocked);
        }

        // Check if user is a global admin
        var userRoles = await _userManager.GetRolesAsync(user);
        var isGlobalAdmin = userRoles.Contains("GlobalAdmin");
        
        UserTeam? userTeam = null;
        
        if (isGlobalAdmin)
        {
            // Global admins can access any team, but we need to check if the team exists
            var teamEntity = await _context.Teams.FindAsync(teamId) ??
                throw new AuthenticationException("Team not found", AuthenticationException.ErrorCodes.TenantNotFound);
            
            // For global admins, create a virtual team membership with full permissions
            userTeam = new UserTeam
            {
                UserId = userId,
                TeamId = teamId,
                Role = TeamRole.TeamOwner, // Global admins get full permissions
                MemberType = MemberType.Coach, // Default member type
                IsActive = true,
                IsDefault = false,
                JoinedOn = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };
        }
        else
        {
            // Regular users must have explicit team membership
            userTeam = await _context.UserTeams
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId && ut.IsActive) ??
                throw new AuthenticationException("Access denied to team", AuthenticationException.ErrorCodes.TenantNotFound);
        }

        // Update last login
        user.LastLoginOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Get all active team memberships for the user
        var teams = await GetAllUserTeamMemberships(userId);
        
        // Generate tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);
        var refreshToken = await CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsGlobalAdmin = isGlobalAdmin,
            Teams = teams,
            RequiresEmailConfirmation = false
        };
    }

    public async Task<AuthResponseDto> RefreshContextAsync(string subdomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain);

        // Get current user from HTTP context
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new AuthenticationException("User not authenticated", AuthenticationException.ErrorCodes.InvalidCredentials);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString()) ??
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);

        if (!user.IsActive)
        {
            throw new AuthenticationException("Account is locked", AuthenticationException.ErrorCodes.AccountLocked);
        }

        // Check if user is a global admin
        var userRoles = await _userManager.GetRolesAsync(user);
        var isGlobalAdmin = userRoles.Contains("GlobalAdmin");

        // Validate subdomain-specific access only when needed
        if (subdomain == "app" && !isGlobalAdmin)
        {
            throw new UnauthorizedAccessException("Global admin privileges required for app subdomain");
        }
        
        if (subdomain != "app" && subdomain != "www" && subdomain != "localhost")
        {
            // For team subdomains, validate team exists and user has access
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain && !t.IsDeleted) ??
                throw new AuthenticationException($"Team with subdomain '{subdomain}' not found", AuthenticationException.ErrorCodes.TenantNotFound);

            if (!isGlobalAdmin)
            {
                // Regular users must have explicit team membership
                var hasAccess = await _context.UserTeams
                    .AnyAsync(ut => ut.UserId == userId && ut.TeamId == team.Id && ut.IsActive);
                    
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"Access denied to team with subdomain '{subdomain}'");
                }
            }
        }

        // Update last login
        user.LastLoginOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Always get user's complete team memberships regardless of subdomain
        var teams = await GetAllUserTeamMemberships(userId);

        // Generate consistent tokens with all user data
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);
        var refreshToken = await CreateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsGlobalAdmin = isGlobalAdmin,
            Teams = teams, // Always the same regardless of subdomain
            RequiresEmailConfirmation = false
        };
    }
} 