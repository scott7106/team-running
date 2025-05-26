using System;

namespace TeamStride.Domain.Identity;

public class UserTenant
{
    public required string UserId { get; set; }
    public required string TenantId { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    
    public virtual ApplicationUser? User { get; set; }
    public virtual Tenant? Tenant { get; set; }
} 