using TeamStride.Domain.Common;

namespace TeamStride.Domain.Entities;

public class TenantUser : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public TenantRole Role { get; set; }
    public bool IsDefault { get; set; }
    public DateTime JoinedAt { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public enum TenantRole
{
    Host,
    Admin,
    Coach,
    Athlete,
    Parent
} 