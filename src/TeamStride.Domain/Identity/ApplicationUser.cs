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

    // New fields
    public DateTime? LastActivityOn { get; set; }
    public DateTime? ForceLogoutAfter { get; set; }

    // Navigation properties
    public virtual ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
} 