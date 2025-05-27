using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class TeamManagementDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public TeamStatus Status { get; set; }
    public TeamTier Tier { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    
    // Owner information
    public TeamMemberDto? Owner { get; set; }
    
    // Statistics
    public int MemberCount { get; set; }
    public int AthleteCount { get; set; }
    public int AdminCount { get; set; }
    
    // Pending transfers
    public bool HasPendingOwnershipTransfer { get; set; }
}

public class CreateTeamDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string Name { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Subdomain can only contain lowercase letters, numbers, and hyphens")]
    public required string Subdomain { get; set; }
    
    [Required]
    [EmailAddress]
    public required string OwnerEmail { get; set; }
    
    public string? OwnerFirstName { get; set; }
    public string? OwnerLastName { get; set; }
    
    public TeamTier Tier { get; set; } = TeamTier.Free;
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
}

public class UpdateTeamDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }
    
    public TeamStatus? Status { get; set; }
    public DateTime? ExpiresOn { get; set; }
}

public class TransferOwnershipDto
{
    [Required]
    [EmailAddress]
    public required string NewOwnerEmail { get; set; }
    
    public string? NewOwnerFirstName { get; set; }
    public string? NewOwnerLastName { get; set; }
    
    [StringLength(500)]
    public string? Message { get; set; }
    
    // For team owners transferring to existing team members
    public Guid? ExistingMemberId { get; set; }
}

public class UpdateSubscriptionDto
{
    [Required]
    public required TeamTier NewTier { get; set; }
    
    public DateTime? ExpiresOn { get; set; }
}

public class UpdateTeamBrandingDto
{
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? PrimaryColor { get; set; }
    
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? SecondaryColor { get; set; }
    
    [Url]
    public string? LogoUrl { get; set; }
}

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public TeamRole Role { get; set; }
    public DateTime JoinedOn { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwner { get; set; }
}

public class TeamTierLimitsDto
{
    public TeamTier Tier { get; set; }
    public int MaxAthletes { get; set; }
    public int MaxAdmins { get; set; }
    public int MaxCoaches { get; set; }
    public bool AllowCustomBranding { get; set; }
    public bool AllowAdvancedReporting { get; set; }
} 