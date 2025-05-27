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
        ArgumentNullException.ThrowIfNull(request.TeamId);

        var user = await _userManager.FindByEmailAsync(request.Email) ??
            throw new AuthenticationException("Invalid credentials", AuthenticationException.ErrorCodes.InvalidCredentials);

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

        // Get team and role
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == user.Id && (ut.TeamId == null || ut.TeamId == request.TeamId)) ??
            throw new AuthenticationException("Invalid team", AuthenticationException.ErrorCodes.TenantNotFound);

        // Update last login
        user.LastLoginOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, userTeam.TeamId, userTeam.Role);
        var refreshToken = await CreateRefreshTokenAsync(user, userTeam.TeamId ?? Guid.Empty);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TeamId = userTeam.TeamId,
            Role = userTeam.Role,
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

        // Create user-team relationship
        var userTeam = new UserTeam
        {
            UserId = user.Id,
            TeamId = request.TeamId,
            Role = request.Role,
            IsActive = true,
            IsDefault = true,
            JoinedOn = DateTime.UtcNow
        };
        _context.UserTeams.Add(userTeam);
        await _context.SaveChangesAsync();

        // Generate email confirmation token and send email
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = $"https://teamstride.com/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);

        // Generate tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, request.TeamId, request.Role);
        var refreshToken = await CreateRefreshTokenAsync(user, request.TeamId);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TeamId = request.TeamId,
            Role = request.Role,
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

        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == token.UserId && ut.TeamId == token.TeamId) ??
            throw new AuthenticationException("Invalid team access", AuthenticationException.ErrorCodes.TenantNotFound);

        // Generate new tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(token.User!, token.TeamId, userTeam.Role);
        var newRefreshToken = await CreateRefreshTokenAsync(token.User!, token.TeamId);

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
            TeamId = token.TeamId,
            Role = userTeam.Role,
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
        var resetLink = $"https://teamstride.com/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";
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

            // Create user-team relationship with default role
            var newUserTeam = new UserTeam
            {
                UserId = user.Id,
                TeamId = request.TeamId,
                Role = TeamRole.TeamMember, // Default role for external users
                IsActive = true
            };
            _context.UserTeams.Add(newUserTeam);
            await _context.SaveChangesAsync();
        }

        // Get team and role
        var teamId = request.TeamId;
        var userTeam = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == user!.Id && ut.TeamId == teamId) ??
            throw new AuthenticationException("Invalid team", AuthenticationException.ErrorCodes.TenantNotFound);

        // Update last login
        user!.LastLoginOn = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var jwtToken = await _jwtTokenService.GenerateJwtTokenAsync(user, teamId, userTeam.Role);
        var refreshToken = await CreateRefreshTokenAsync(user, teamId);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TeamId = teamId,
            Role = userTeam.Role,
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

    private async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user, Guid? teamId)
    {
        var refreshToken = new RefreshToken
        {
            Token = _jwtTokenService.GenerateRefreshToken(),
            UserId = user.Id,
            TeamId = teamId ?? Guid.Empty,
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }
} 