using TeamStride.Domain.Common;
using TeamStride.Domain.Identity;
namespace TeamStride.Domain.Entities;

public class UserTeam : AuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public Guid? TeamId { get; set; }
    public TeamRole Role { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedOn { get; set; }
    
    // Navigation properties
    public virtual Team? Team { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
}

public enum TeamRole
{
    GlobalAdmin,
    Host,
    Admin,
    Coach,
    Athlete,
    Parent
} 