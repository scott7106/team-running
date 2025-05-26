using TeamStride.Domain.Common;
using TeamStride.Domain.Identity;
namespace TeamStride.Domain.Entities;

public class UserTenant : AuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public TenantRole Role { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedOn { get; set; }
    
    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
}

public enum TenantRole
{
    GlobalAdmin,
    Host,
    Admin,
    Coach,
    Athlete,
    Parent
} 