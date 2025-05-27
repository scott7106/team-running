using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using TeamStride.Domain.Common;
using TeamStride.Domain.Entities;

namespace TeamStride.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>, IAuditedEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Guid? DefaultTeamId { get; set; }
    public bool IsGlobalAdmin { get; private set; }
    
    // Audit fields
    public DateTime CreatedOn { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public Guid? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Status fields
    public bool IsActive { get; set; }
    public DateTime? LastLoginOn { get; set; }
    public UserStatus Status { get; set; }

    // Navigation properties
    public virtual ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public void SetGlobalAdmin(bool isGlobalAdmin)
    {
        if (isGlobalAdmin && IsGlobalAdmin)
            throw new InvalidOperationException("User is already a global admin.");
        
        if (!isGlobalAdmin && !IsGlobalAdmin)
            throw new InvalidOperationException("User is not a global admin.");
            
        IsGlobalAdmin = isGlobalAdmin;
        ModifiedOn = DateTime.UtcNow;
    }
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
} 