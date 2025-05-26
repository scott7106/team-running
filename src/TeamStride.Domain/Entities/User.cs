using TeamStride.Domain.Common;

namespace TeamStride.Domain.Entities;

public class User : AuditedEntity<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LastLoginOn { get; set; }
    public UserStatus Status { get; set; }
    
    // Navigation properties
    public virtual ICollection<TenantUser> Tenants { get; set; } = new List<TenantUser>();
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
} 