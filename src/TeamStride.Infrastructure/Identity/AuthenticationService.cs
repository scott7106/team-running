using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using TeamStride.Application.Authentication;
using TeamStride.Application.Authentication.Dtos;
using TeamStride.Application.Authentication.Services;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Email;

namespace TeamStride.Infrastructure.Identity;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IdentityContext _context;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IExternalAuthService _externalAuthService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IdentityContext context,
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

        var user = await _userManager.FindByEmailAsync(request.Email) ?? 
            throw new AuthenticationException("Invalid email or password", AuthenticationException.ErrorCodes.InvalidCredentials);

        if (!user.IsActive)
        {
            throw new AuthenticationException("Account is locked", AuthenticationException.ErrorCodes.AccountLocked);
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AuthenticationException("Invalid email or password", AuthenticationException.ErrorCodes.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
        {
            throw new AuthenticationException("Email not confirmed", AuthenticationException.ErrorCodes.EmailNotConfirmed);
        }

        // Get tenant and role
        var tenantId = request.TenantId ?? user.DefaultTenantId;
        var userTenant = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TenantId == tenantId) ??
            throw new AuthenticationException("Invalid tenant", AuthenticationException.ErrorCodes.TenantNotFound);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var jwtToken = _jwtTokenService.GenerateJwtToken(user, tenantId, userTenant.Role);
        var refreshToken = await CreateRefreshTokenAsync(user, tenantId);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TenantId = tenantId,
            Role = userTenant.Role,
            RequiresEmailConfirmation = false
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Email);
        ArgumentNullException.ThrowIfNull(request.Password);
        ArgumentNullException.ThrowIfNull(request.TenantId);
        ArgumentNullException.ThrowIfNull(request.Role);

        // Validate tenant
        var tenant = await _context.Tenants.FindAsync(request.TenantId) ??
            throw new AuthenticationException("Invalid tenant", AuthenticationException.ErrorCodes.TenantNotFound);

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
            DefaultTenantId = request.TenantId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException($"Registration failed: {errors}");
        }

        // Create user-tenant relationship
        var userTenant = new UserTenant
        {
            UserId = user.Id,
            TenantId = request.TenantId,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserTenants.Add(userTenant);
        await _context.SaveChangesAsync();

        // Generate email confirmation token and send email
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = $"https://{tenant.Subdomain}.teamstride.com/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);

        // Generate tokens
        var jwtToken = _jwtTokenService.GenerateJwtToken(user, request.TenantId, request.Role);
        var refreshToken = await CreateRefreshTokenAsync(user, request.TenantId);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TenantId = request.TenantId,
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

        var userTenant = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == token.UserId && ut.TenantId == token.TenantId) ??
            throw new AuthenticationException("Invalid tenant access", AuthenticationException.ErrorCodes.TenantNotFound);

        // Generate new tokens
        var jwtToken = _jwtTokenService.GenerateJwtToken(token.User, token.TenantId, userTenant.Role);
        var newRefreshToken = await CreateRefreshTokenAsync(token.User, token.TenantId);

        // Revoke old refresh token
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = newRefreshToken.Token;
        token.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = newRefreshToken.Token,
            Email = token.User.Email ?? string.Empty,
            FirstName = token.User.FirstName,
            LastName = token.User.LastName,
            TenantId = token.TenantId,
            Role = userTenant.Role,
            RequiresEmailConfirmation = false
        };
    }

    public async Task<bool> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Return true to prevent email enumeration
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var tenant = await _context.Tenants.FindAsync(user.DefaultTenantId);
        var resetLink = $"https://{tenant.Subdomain}.teamstride.com/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        
        await _emailService.SendPasswordResetAsync(email, resetLink);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new AuthenticationException("User not found", AuthenticationException.ErrorCodes.UserNotFound);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException($"Password reset failed: {errors}");
        }

        // Revoke all refresh tokens
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var refreshToken in tokens)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReasonRevoked = "Password changed";
            refreshToken.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
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
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var refreshToken in tokens)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReasonRevoked = "Password changed";
            refreshToken.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LogoutAsync(string userId)
    {
        // Revoke all active refresh tokens
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
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
        ArgumentNullException.ThrowIfNull(request.TenantId);

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
                EmailConfirmed = true, // External providers have already verified the email
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                DefaultTenantId = request.TenantId
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AuthenticationException($"External registration failed: {errors}");
            }

            // Create user-tenant relationship with default role
            var newUserTenant = new UserTenant
            {
                UserId = user.Id,
                TenantId = request.TenantId,
                Role = "Athlete", // Default role for external users
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.UserTenants.Add(newUserTenant);
            await _context.SaveChangesAsync();
        }

        // Get tenant and role
        var tenantId = request.TenantId;
        var userTenant = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TenantId == tenantId) ??
            throw new AuthenticationException("Invalid tenant", AuthenticationException.ErrorCodes.TenantNotFound);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var jwtToken = _jwtTokenService.GenerateJwtToken(user, tenantId, userTenant.Role);
        var refreshToken = await CreateRefreshTokenAsync(user, tenantId);

        return new AuthResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken.Token,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TenantId = tenantId,
            Role = userTenant.Role,
            RequiresEmailConfirmation = false
        };
    }

    public async Task<string> GetExternalLoginUrlAsync(string provider, string? tenantId = null)
    {
        var baseUrl = _configuration["Authentication:ExternalProviders:BaseUrl"] ?? 
            throw new InvalidOperationException("Authentication:ExternalProviders:BaseUrl not configured");
        var clientId = _configuration[$"Authentication:ExternalProviders:{provider}:ClientId"] ?? 
            throw new InvalidOperationException($"Authentication:ExternalProviders:{provider}:ClientId not configured");
        var redirectUri = await GetExternalLoginCallbackUrlAsync(provider);

        var state = tenantId ?? string.Empty;

        switch (provider.ToLowerInvariant())
        {
            case "microsoft":
                return $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize" +
                       $"?client_id={clientId}" +
                       $"&response_type=code" +
                       $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                       $"&scope=openid email profile" +
                       $"&state={state}";

            case "google":
                return $"https://accounts.google.com/o/oauth2/v2/auth" +
                       $"?client_id={clientId}" +
                       $"&response_type=code" +
                       $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                       $"&scope=openid email profile" +
                       $"&state={state}";

            case "facebook":
                return $"https://www.facebook.com/v12.0/dialog/oauth" +
                       $"?client_id={clientId}" +
                       $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                       $"&scope=email public_profile" +
                       $"&state={state}";

            case "twitter":
                return $"https://twitter.com/i/oauth2/authorize" +
                       $"?client_id={clientId}" +
                       $"&response_type=code" +
                       $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                       $"&scope=users.read tweet.read" +
                       $"&state={state}";

            default:
                throw new AuthenticationException($"Unsupported provider: {provider}");
        }
    }

    public async Task<string> GetExternalLoginCallbackUrlAsync(string provider)
    {
        var baseUrl = _configuration["Authentication:ExternalProviders:BaseUrl"] ?? 
            throw new InvalidOperationException("Authentication:ExternalProviders:BaseUrl not configured");
        return await Task.FromResult($"{baseUrl}/api/authentication/external-login/{provider.ToLowerInvariant()}/callback");
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(tenantId);

        var token = new RefreshToken
        {
            Token = _jwtTokenService.GenerateRefreshToken(),
            UserId = user.Id,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        return token;
    }
} 