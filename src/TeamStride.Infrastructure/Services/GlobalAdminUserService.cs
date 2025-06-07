using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Users.Dtos;
using TeamStride.Application.Users.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;

namespace TeamStride.Infrastructure.Services;

/// <summary>
/// Implementation of global admin user management service.
/// All operations require global admin privileges and bypass normal user access restrictions.
/// </summary>
public class GlobalAdminUserService : IGlobalAdminUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly ILogger<GlobalAdminUserService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GlobalAdminUserService(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IMapper mapper,
        ILogger<GlobalAdminUserService> logger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<GlobalAdminUserDto>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null,
        UserStatus? status = null,
        bool? isActive = null,
        bool isDeleted = false)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var query = _context.Users
            .IgnoreQueryFilters() // Bypass global query filters
            .Where(u => u.IsDeleted == isDeleted);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.ToLower();
            query = query.Where(u => 
                u.Email!.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Id.ToString().Contains(search));
        }

        // Apply status filters
        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // Order by creation date (newest first)
        query = query.OrderByDescending(u => u.CreatedOn);

        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.UserTeams)
                .ThenInclude(ut => ut.Team)
            .ToListAsync();

        var userDtos = new List<GlobalAdminUserDto>();
        foreach (var user in users)
        {
            var dto = await MapUserToGlobalAdminUserDto(user);
            userDtos.Add(dto);
        }

        return new PaginatedList<GlobalAdminUserDto>(userDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedList<DeletedUserDto>> GetDeletedUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchQuery = null)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var query = _context.Users
            .IgnoreQueryFilters() // Bypass global query filters
            .Where(u => u.IsDeleted);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.ToLower();
            query = query.Where(u => 
                u.Email!.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Id.ToString().Contains(search));
        }

        // Order by deletion date (newest first)
        query = query.OrderByDescending(u => u.DeletedOn);

        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.UserTeams!)
                .ThenInclude(ut => ut.Team)
            .ToListAsync();

        var userDtos = new List<DeletedUserDto>();
        foreach (var user in users)
        {
            var dto = await MapUserToDeletedUserDto(user);
            userDtos.Add(dto);
        }

        return new PaginatedList<DeletedUserDto>(userDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<GlobalAdminUserDto> GetUserByIdAsync(Guid userId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _context.Users
            .IgnoreQueryFilters() // Bypass global query filters
            .Include(u => u.UserTeams)
                .ThenInclude(ut => ut.Team)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        return await MapUserToGlobalAdminUserDto(user);
    }

    public async Task<GlobalAdminUserDto> CreateUserAsync(GlobalAdminCreateUserDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        // Check if email is already taken
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
        {
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use");
        }

        // Validate application roles
        foreach (var roleName in dto.ApplicationRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                throw new InvalidOperationException($"Role '{roleName}' does not exist");
            }
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = true, // Auto-confirm emails for admin-created accounts
            IsActive = true,
            Status = UserStatus.Active
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Assign application roles
        foreach (var roleName in dto.ApplicationRoles)
        {
            await _userManager.AddToRoleAsync(user, roleName);
        }

        // If password change is required, set user token
        if (dto.RequirePasswordChange)
        {
            await _userManager.SetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange", "true");
        }

        _logger.LogInformation("Global admin {AdminId} created user {UserId} with email {Email}", 
            _currentUserService.UserId, user.Id, user.Email);

        return await GetUserByIdAsync(user.Id);
    }

    public async Task<GlobalAdminUserDto> UpdateUserAsync(Guid userId, GlobalAdminUpdateUserDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // Check if email is available (excluding current user)
        if (!await IsEmailAvailableAsync(dto.Email, userId))
        {
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use");
        }

        // Update user properties
        user.Email = dto.Email;
        user.UserName = dto.Email; // Keep username in sync with email
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Status = dto.Status;
        user.IsActive = dto.IsActive;
        user.EmailConfirmed = dto.EmailConfirmed;
        user.PhoneNumberConfirmed = dto.PhoneNumberConfirmed;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        _logger.LogInformation("Global admin {AdminId} updated user {UserId}", 
            _currentUserService.UserId, userId);

        return await GetUserByIdAsync(userId);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        if (user.IsDeleted)
        {
            throw new InvalidOperationException("User is already deleted");
        }

        // Soft delete the user
        user.IsDeleted = true;
        user.DeletedOn = DateTime.UtcNow;
        user.DeletedBy = _currentUserService.UserId;
        user.IsActive = false;

        // Deactivate all user-team relationships
        var userTeams = await _context.UserTeams
            .Where(ut => ut.UserId == userId)
            .ToListAsync();

        foreach (var userTeam in userTeams)
        {
            userTeam.IsActive = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Global admin {AdminId} soft deleted user {UserId}", 
            _currentUserService.UserId, userId);
    }

    public async Task PurgeUserAsync(Guid userId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserTeams)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove refresh tokens
                _context.RefreshTokens.RemoveRange(user.RefreshTokens);

                // Remove user-team relationships
                _context.UserTeams.RemoveRange(user.UserTeams);

                // Remove user roles
                var userRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
                _context.UserRoles.RemoveRange(userRoles);

                // Remove user claims
                var userClaims = await _context.UserClaims.Where(uc => uc.UserId == userId).ToListAsync();
                _context.UserClaims.RemoveRange(userClaims);

                // Remove user logins
                var userLogins = await _context.UserLogins.Where(ul => ul.UserId == userId).ToListAsync();
                _context.UserLogins.RemoveRange(userLogins);

                // Remove user tokens
                var userTokens = await _context.UserTokens.Where(ut => ut.UserId == userId).ToListAsync();
                _context.UserTokens.RemoveRange(userTokens);

                // Finally, remove the user
                _context.Users.Remove(user);

                await _context.SaveChangesWithoutAuditAsync();
                await transaction.CommitAsync();

                _logger.LogWarning("Global admin {AdminId} permanently deleted user {UserId} with email {Email}", 
                    _currentUserService.UserId, userId, user.Email);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<GlobalAdminUserDto> RecoverUserAsync(Guid userId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        if (!user.IsDeleted)
        {
            throw new InvalidOperationException("User is not deleted");
        }

        // Restore the user
        user.IsDeleted = false;
        user.DeletedOn = null;
        user.DeletedBy = null;
        user.IsActive = true;
        user.Status = UserStatus.Active;

        // Reactivate user-team relationships
        var userTeams = await _context.UserTeams
            .Where(ut => ut.UserId == userId)
            .ToListAsync();

        foreach (var userTeam in userTeams)
        {
            userTeam.IsActive = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Global admin {AdminId} recovered user {UserId}", 
            _currentUserService.UserId, userId);

        return await GetUserByIdAsync(userId);
    }

    public async Task<GlobalAdminUserDto> ResetLockoutAsync(Guid userId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // Reset lockout
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("Global admin {AdminId} reset lockout for user {UserId}", 
            _currentUserService.UserId, userId);

        return await GetUserByIdAsync(userId);
    }

    public async Task<PasswordResetResultDto> ResetPasswordAsync(Guid userId, GlobalAdminResetPasswordDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        string newPassword;
        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            newPassword = dto.NewPassword;
        }
        else
        {
            // Generate a temporary password
            newPassword = GenerateTemporaryPassword();
        }

        // Remove current password and set new one
        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to reset password: {errors}");
        }

        // Force password change on next login if specified
        if (dto.RequirePasswordChange)
        {
            await _userManager.SetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange", "true");
        }

        // Reset lockout if user was locked
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("Global admin {AdminId} reset password for user {UserId}", 
            _currentUserService.UserId, userId);

        return new PasswordResetResultDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TemporaryPassword = dto.NewPassword == null ? newPassword : null,
            RequirePasswordChange = dto.RequirePasswordChange,
            PasswordSentByEmail = dto.SendPasswordByEmail,
            PasswordSentBySms = dto.SendPasswordBySms,
            ResetTimestamp = DateTime.UtcNow
        };
    }

    public async Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email == email);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<List<ApplicationRoleDto>> GetApplicationRolesAsync()
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var roles = await _context.Roles.ToListAsync();
        var roleDtos = new List<ApplicationRoleDto>();

        foreach (var role in roles)
        {
            var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id);
            roleDtos.Add(new ApplicationRoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                NormalizedName = role.NormalizedName,
                UserCount = userCount
            });
        }

        return roleDtos.OrderBy(r => r.Name).ToList();
    }

    public async Task<List<string>> GetUserApplicationRolesAsync(Guid userId)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<GlobalAdminUserDto> AddUserToRoleAsync(Guid userId, string roleName)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException($"Role '{roleName}' does not exist");
        }

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            throw new InvalidOperationException($"User is already in role '{roleName}'");
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to add user to role: {errors}");
        }

        _logger.LogInformation("Global admin {AdminId} added user {UserId} to role {RoleName}", 
            _currentUserService.UserId, userId, roleName);

        return await GetUserByIdAsync(userId);
    }

    public async Task<GlobalAdminUserDto> RemoveUserFromRoleAsync(Guid userId, string roleName)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            throw new InvalidOperationException($"User is not in role '{roleName}'");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to remove user from role: {errors}");
        }

        _logger.LogInformation("Global admin {AdminId} removed user {UserId} from role {RoleName}", 
            _currentUserService.UserId, userId, roleName);

        return await GetUserByIdAsync(userId);
    }

    public async Task<GlobalAdminUserDto> SetUserRolesAsync(Guid userId, SetUserRolesDto dto)
    {
        await _authorizationService.RequireGlobalAdminAsync();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // Validate all roles exist
        foreach (var roleName in dto.RoleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                throw new InvalidOperationException($"Role '{roleName}' does not exist");
            }
        }

        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Remove all current roles
        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to remove current roles: {errors}");
            }
        }

        // Add new roles
        if (dto.RoleNames.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, dto.RoleNames);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to add new roles: {errors}");
            }
        }

        var roleList = string.Join(", ", dto.RoleNames);
        _logger.LogInformation("Global admin {AdminId} set roles for user {UserId} to: {Roles}", 
            _currentUserService.UserId, userId, roleList);

        return await GetUserByIdAsync(userId);
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        await _authorizationService.RequireGlobalAdminAsync();
        return await _roleManager.RoleExistsAsync(roleName);
    }

    #region Private Helper Methods

    private async Task<GlobalAdminUserDto> MapUserToGlobalAdminUserDto(ApplicationUser user)
    {
        // Get application roles
        var applicationRoles = await _userManager.GetRolesAsync(user);
        
        // Get team memberships
        var teamMemberships = user.UserTeams?.Where(ut => ut.IsActive).Select(ut => new UserTeamSummaryDto
        {
            UserTeamId = ut.Id,
            TeamId = ut.TeamId ?? Guid.Empty,
            TeamName = ut.Team?.Name ?? "Unknown Team",
            TeamSubdomain = ut.Team?.Subdomain ?? string.Empty,
            Role = ut.Role,
            MemberType = ut.MemberType,
            IsActive = ut.IsActive,
            JoinedOn = ut.JoinedOn
        }).ToList() ?? new List<UserTeamSummaryDto>();

        // Get default team name
        var defaultTeamName = user.DefaultTeamId.HasValue 
            ? teamMemberships.FirstOrDefault(tm => tm.TeamId == user.DefaultTeamId)?.TeamName
            : null;

        // Get audit information with names
        var createdByName = user.CreatedBy.HasValue 
            ? await GetUserDisplayNameAsync(user.CreatedBy.Value) 
            : null;
        var modifiedByName = user.ModifiedBy.HasValue 
            ? await GetUserDisplayNameAsync(user.ModifiedBy.Value) 
            : null;
        var deletedByName = user.DeletedBy.HasValue 
            ? await GetUserDisplayNameAsync(user.DeletedBy.Value) 
            : null;

        return new GlobalAdminUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = $"{user.LastName}, {user.FirstName}",
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            IsActive = user.IsActive,
            IsDeleted = user.IsDeleted,
            ApplicationRoles = applicationRoles.ToList(),
            IsGlobalAdmin = applicationRoles.Contains("GlobalAdmin"),
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            LastLoginOn = user.LastLoginOn,
            DefaultTeamId = user.DefaultTeamId,
            DefaultTeamName = defaultTeamName,
            CreatedOn = user.CreatedOn,
            CreatedBy = user.CreatedBy,
            CreatedByName = createdByName,
            ModifiedOn = user.ModifiedOn,
            ModifiedBy = user.ModifiedBy,
            ModifiedByName = modifiedByName,
            DeletedOn = user.DeletedOn,
            DeletedBy = user.DeletedBy,
            DeletedByName = deletedByName,
            TeamCount = teamMemberships.Count,
            TeamMemberships = teamMemberships
        };
    }

    private async Task<DeletedUserDto> MapUserToDeletedUserDto(ApplicationUser user)
    {
        // Get application roles
        var applicationRoles = await _userManager.GetRolesAsync(user);
        
        // Get team names at time of deletion
        var teamNames = user.UserTeams?.Select(ut => ut.Team?.Name ?? "Unknown Team").ToList() ?? new List<string>();
        
        // Get deletion info
        var deletedByName = user.DeletedBy.HasValue 
            ? await GetUserDisplayNameAsync(user.DeletedBy.Value) 
            : null;

        return new DeletedUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = $"{user.LastName}, {user.FirstName}",
            Status = user.Status,
            ApplicationRoles = applicationRoles.ToList(),
            IsGlobalAdmin = applicationRoles.Contains("GlobalAdmin"),
            DeletedOn = user.DeletedOn ?? DateTime.MinValue,
            DeletedBy = user.DeletedBy,
            DeletedByName = deletedByName,
            CreatedOn = user.CreatedOn,
            LastLoginOn = user.LastLoginOn,
            TeamCount = teamNames.Count,
            TeamNames = teamNames
        };
    }

    private async Task<string?> GetUserDisplayNameAsync(Guid userId)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync();

        return user != null ? $"{user.LastName}, {user.FirstName}" : null;
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        const string symbols = "!@#$%&*";
        
        var random = new Random();
        var password = new StringBuilder();

        // Ensure at least one uppercase, one lowercase, one digit, and one symbol
        password.Append(chars[random.Next(0, 26)]); // Uppercase
        password.Append(chars[random.Next(26, 52)]); // Lowercase  
        password.Append(chars[random.Next(52, chars.Length)]); // Digit
        password.Append(symbols[random.Next(symbols.Length)]); // Symbol

        // Fill the rest randomly
        for (int i = 4; i < 12; i++)
        {
            var allChars = chars + symbols;
            password.Append(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password
        var shuffled = password.ToString().ToCharArray();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return new string(shuffled);
    }

    #endregion
} 