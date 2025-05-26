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
    public required Guid DefaultTenantId { get; set; }
    
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
    public virtual ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
} 