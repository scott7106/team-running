using TeamStride.Domain.Identity;

namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// User DTO for global admin operations with complete user information.
/// </summary>
public class GlobalAdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserStatus Status { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    // Application role information
    public List<string> ApplicationRoles { get; set; } = new();
    public bool IsGlobalAdmin { get; set; }
    
    // Account status information
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    
    // Activity information
    public DateTime? LastLoginOn { get; set; }
    public Guid? DefaultTeamId { get; set; }
    public string? DefaultTeamName { get; set; }
    
    // Audit information
    public DateTime CreatedOn { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public Guid? ModifiedBy { get; set; }
    public string? ModifiedByName { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }
    public string? DeletedByName { get; set; }
    
    // Team memberships summary
    public int TeamCount { get; set; }
    public List<UserTeamSummaryDto> TeamMemberships { get; set; } = new();
} 